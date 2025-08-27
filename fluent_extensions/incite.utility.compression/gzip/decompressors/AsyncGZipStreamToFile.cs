using contoso.utility.compression.deflate;
using contoso.functional.patterns.result;
using contoso.utility.compression.entities;

namespace contoso.utility.compression.gzip.decompressors;

/// <summary>Utility to asynchronously decompress the configured source stream to the configured output file.</summary>
/// <example>
///     <code>
///         var asyncReader = GzipReader.CreateFor(zippedStream).AndExtractToFileAsync('path_to_output_file');
///     </code>
/// </example>
internal sealed class AsyncGZipStreamToFile : UnzipAsyncToFile
{
    readonly StreamToFile _io;

    internal AsyncGZipStreamToFile(in StreamToFile ioPair)
        : base()
    {
        _io = ioPair;

        Result<GZipFormatData> formatValidation =
            _io.Verify()
               .MapResultValueTo((io) => io.Input)
               .OnSuccess(GZipReader.TryReadHeaderAndFooter)
               .AndEither(successAction: HandleCompletedRead,
                          failureAction: HandleFailedRead);

        GZipFormatData HandleCompletedRead(GZipFormatData gzipData)
        {
            // capture input state for debugging
            OperationResult.InputState = new
                (
                    InputFile: null,
                    OutputFile: _io.Output.FullPath,
                    InputIsStream: true,
                    OutputIsStream: false,
                    Header: gzipData.HeaderDisplay,
                    Footer: gzipData.FooterDisplay
                );

            // although enough header and footer bytes were read,
            // fail the overall operation result if the header does not contain a valid gzip signature
            if (!gzipData.HasGZipSignature)
                OperationResult.Fail(GZipReader.NoGzipSignatureError);

            return gzipData;
        }

        void HandleFailedRead(IErrorResult error)
        {
            // capture input state for debugging
            OperationResult.InputState = new
                (
                    InputFile: null,
                    OutputFile: _io.Output.FullPath,
                    InputIsStream: true,
                    OutputIsStream: false,
                    Header: null,
                    Footer: null
                );

            // fail operation with first error
            OperationResult.Fail(error.ErrorMessage);
        }

        // note: we don't explicitly succeed the OperationResult from the constructor, only explicitly fail
    }

    /// <summary>Asynchronously decompress the configured source stream to the configured output file.</summary>
    /// <returns>
    /// A <see cref="DecompressToFileResult"/> where 'Success' is:
    /// True if the stream is unzipped without error and the output file exists on disk. A zero length output returns false because it will be removed.
    /// Otherwise false. Failure reason and/or exception will be found in <see cref="DecompressToStreamResult.Error"/>.
    /// </returns>
    public override async Task<DecompressToFileResult> DecompressAsync()
    {
        if (!OperationResult.Ok)
            return OperationResult;

        await DecompressInputStreamAsync(_io)
              .AndAsyncEither(successAction: SetOperationSuccess,
                              failureAction: SetOperationFailure);

        return OperationResult;
    }

    /// <summary>
    /// Asynchronously decompress the configured source stream to the configured output file
    /// ignoring the gzip header and footer and applying the deflate algorithm to the compressed data only.
    /// </summary>
    /// <returns>
    /// A <see cref="DecompressToFileResult"/> where 'Success' is:
    /// True if the stream is unzipped without error and the output file exists on disk. A zero length output returns false because it will be removed.
    /// Otherwise false. Failure reason and/or exception will be found in <see cref="DecompressToStreamResult.Error"/>.
    /// </returns>
    public override async Task<DecompressToFileResult> DecompressAsyncWithoutCRC()
    {
        if (!OperationResult.Ok)
            return OperationResult;

        OperationResult.SubLog.Debug(GZipReader.DecompressStartMsgNoCRC);

        // make a copy of the input stream trimmed of header and footer, leaving only raw deflate data
        // TODO if the fourth byte is 0x08 (fourth bit is set) then we need to also skip the file name up to and including first 00 terminator;
        //      however, gzip 1.5 on oasis does not seem to do this (new versions might)
        DisposableResult<MemoryStream> trimmedInputResult = await _io.Input
                                                                     .TrimToMemoryAsync(GZipFormatData.GZipHeaderLength, GZipFormatData.GZipFooterLength)
                                                                     .ConfigureAwait(false);
        try
        {
            await trimmedInputResult.AsResult()
                                    .MapResultValueTo((copy) => new StreamToFile(copy, _io.Output))
                                    .OnSuccessAsync(DeflateTrimmedInputAsync)
                                    .OnSuccessAsync(VerifyOutputFile)
                                    .AndAsyncEither(successAction: SetOperationSuccess,
                                                    failureAction: SetOperationFailure)
                                    .ConfigureAwait(false);
        }
        finally
        {
            // close the copy of the input stream trimmed of header and footer
            try { trimmedInputResult.DisposableValue.Dispose(); }
            catch { }
        }

        return OperationResult;
    }

    /// <summary>
    /// Attempt to asynchronously decompress the source stream using the gzip library, and if the operation fails,
    /// attempt a second pass ignoring the gzip header and footer and applying the deflate algorithm to the compressed data only.
    /// </summary>
    /// <returns>
    /// A <see cref="DecompressToFileResult"/> where 'Success' is:
    /// True if the stream is unzipped without error and the output file exists on disk. A zero length output returns false because it will be removed.
    /// Otherwise false. Failure reason and/or exception will be found in <see cref="DecompressToStreamResult.Error"/>.
    /// </returns>
    public override async Task<DecompressToFileResult> DecompressAsyncWithRetry()
    {
        if (!OperationResult.Ok)
            return OperationResult;

        // when executing a retry with streams, we need a copy of the opened input stream,
        // because if the first attempt fails, the input stream will likely be closed by the GZipStream api,
        // and since the caller manages opening and closing the stream, we cannot re-open it ourselves;
        DisposableResult<MemoryStream> copyInputResult = await _io.Input.CopyToMemoryAsync().ConfigureAwait(false);

        try
        {
            var firstResult = await copyInputResult.AsResult()
                                                   .MapResultValueTo((mem) => new StreamToFile(mem, _io.Output))
                                                   .OnSuccessAsync(DecompressInputStreamAsync)
                                                   .AndAsyncEither(successAction: SetOperationSuccess,
                                                                   failureAction: LogFirstPassError)
                                                   .ConfigureAwait(false);

            // if first pass succeeded, we are done; otherwise, invoke the decompression without CRC
            return firstResult.Success ? OperationResult : await DecompressAsyncWithoutCRC().ConfigureAwait(false);
        }
        finally
        {
            // close the copy of the input stream we made for internal use of attempt #1
            try { copyInputResult.DisposableValue.Dispose(); }
            catch { }
        }

        void LogFirstPassError(IErrorResult error) => OperationResult.SubLog.Error(GZipReader.DecompressWithRetryError);
    }

    Task<Result<IOutputFile>> DecompressInputStreamAsync(StreamToFile io)
    {
        OperationResult.SubLog.Debug(GZipReader.DecompressStartMsg);

        return io.Verify()
                 .OnSuccessAsync(InvokeDecompressCoreAsync)
                 .OnSuccessAsync(VerifyOutputFile);

        static async Task<Result<IOutputFile>> InvokeDecompressCoreAsync(StreamToFile io)
        {
            try
            {
                // create the output file stream
                FileStream outputStream;
                await using ((outputStream = io.Output.Create(useAsync: true)).ConfigureAwait(false))
                {
                    // wrap the streams
                    var ioStreams = new StreamToStream(io.Input, outputStream);
                    // decompress the input stream to the output stream and return the output file
                    return await GZipAsyncReader.DecompressCoreAsync(ioStreams)
                                                .MapResultAsyncTo(_ => io.Output);
                }
            }
            catch (Exception ex)
            {
                return Result<IOutputFile>.WithError(Error.ExceptionWasThrown, ex);
            }
        }
    }

    static async Task<Result<IOutputFile>> DeflateTrimmedInputAsync(StreamToFile ioPair)
    {
        try
        {
            Stream trimmedStream;
            await using ((trimmedStream = ioPair.Input.Stream).ConfigureAwait(false))
            {
                var opResult = await DeflateReader.CreateFor(trimmedStream)
                                                  .AndExtractToFileAsync(ioPair.Output.FullPath)
                                                  .DecompressAsync()
                                                  .ConfigureAwait(false);
                return opResult.Success
                       ? Result<IOutputFile>.WithSuccess(ioPair.Output)
                       : Result<IOutputFile>.WithError(Error.DeflateStreamOperationFailed, errorContext: opResult.Error ?? GZipReader.DeflateFailedError);
            }
        }
        catch (Exception deflateEx)
        {
            return Result<IOutputFile>.WithError(Error.ExceptionWasThrown, deflateEx);
        }
    }
}

