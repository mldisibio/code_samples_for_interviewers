using contoso.functional.patterns.result;
using contoso.utility.compression.deflate;
using contoso.utility.compression.entities;

namespace contoso.utility.compression.gzip.decompressors;

/// <summary>Utility to asynchronously decompress the configured source stream to the configured output stream.</summary>
/// <example>
///     <code>
///         var asyncReader = GzipReader.CreateFor(zippedStream).AndExtractToFileAsync(decompressedStream);
///     </code>
/// </example>
internal sealed class AsyncGZipStreamToStream : UnzipAsyncToStream
{
    readonly StreamToStream _io;

    internal AsyncGZipStreamToStream(in StreamToStream ioStreams)
        : base()
    {
        _io = ioStreams;

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
                    OutputFile: null,
                    InputIsStream: true,
                    OutputIsStream: true,
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
                    OutputFile: null,
                    InputIsStream: true,
                    OutputIsStream: true,
                    Header: null,
                    Footer: null
                );

            // fail operation with first error
            OperationResult.Fail(error.ErrorMessage);
        }

        // note: we don't explicitly succeed the OperationResult from the constructor, only explicitly fail
    }

    /// <inheritdoc/>
    public override async Task<DecompressToStreamResult> DecompressAsync()
    {
        if (!OperationResult.Ok)
            return OperationResult;

        await DecompressInputStreamAsync(_io)
              .AndAsyncEither(successAction: SetOperationSuccess,
                              failureAction: SetOperationFailure)
              .ConfigureAwait(false);

        return OperationResult;
    }

    /// <inheritdoc/>
    public override async Task<DecompressToStreamResult> DecompressAsyncWithoutCRC()
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
                                    .MapResultValueTo((copy) => new StreamToStream(copy, _io.Output))
                                    .OnSuccessAsync(DeflateTrimmedInputAsync)
                                    .OnSuccessAsync(VerifyOutputStream)
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

    /// <inheritdoc/>
    public override async Task<DecompressToStreamResult> DecompressAsyncWithRetry()
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
                                                   .MapResultValueTo((copy) => new StreamToStream(copy, _io.Output))
                                                   .OnSuccessAsync(DecompressInputStreamAsync)
                                                   .AndAsyncEither(successAction: SetOperationSuccess,
                                                                   failureAction: HandleFailedFirstPass)
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

        void HandleFailedFirstPass(IErrorResult error)
        {
            // reset output stream position for second attempt
            _io.Output.TryResetPositionToZero();
            OperationResult.SubLog.Error(GZipReader.DecompressWithRetryError);
        }
    }

    Task<Result<IOutputStream>> DecompressInputStreamAsync(StreamToStream io)
    {
        OperationResult.SubLog.Debug(GZipReader.DecompressStartMsg);

        return io.Verify()
                 .OnSuccessAsync(GZipAsyncReader.DecompressCoreAsync)
                 .MapResultValueAsyncTo((pair) => pair.Output)
                 .OnSuccessAsync(VerifyOutputStream);
    }

    static async Task<Result<IOutputStream>> DeflateTrimmedInputAsync(StreamToStream io)
    {
        try
        {
            Stream trimmedStream;
            await using ((trimmedStream = io.Input.Stream).ConfigureAwait(false))
            {
                var opResult = await DeflateReader.CreateFor(trimmedStream)
                                                  .AndExtractToStreamAsync(io.Output.Stream)
                                                  .DecompressAsync()
                                                  .ConfigureAwait(false);
                return opResult.Success
                       ? Result<IOutputStream>.WithSuccess(io.Output)
                       : Result<IOutputStream>.WithError(Error.DeflateStreamOperationFailed, errorContext: opResult.Error ?? GZipReader.DeflateFailedError);
            }
        }
        catch (Exception deflateEx)
        {
            return Result<IOutputStream>.WithError(Error.ExceptionWasThrown, deflateEx);
        }
    }
}

