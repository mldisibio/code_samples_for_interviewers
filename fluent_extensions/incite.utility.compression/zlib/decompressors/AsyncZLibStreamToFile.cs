using contoso.utility.compression.deflate;
using contoso.functional.patterns.result;
using contoso.utility.compression.entities;

namespace contoso.utility.compression.zlib.decompressors;

/// <summary>Utility to asynchronously decompress the configured source stream to the configured output file.</summary>
/// <example>
///     <code>
///         var asyncReader = ZLibReader.CreateFor(zlibStream).AndExtractToFileAsync('path_to_output_file');
///     </code>
/// </example>
internal sealed class AsyncZLibStreamToFile : ZInflateAsyncToFile
{
    readonly StreamToFile _io;

    internal AsyncZLibStreamToFile(in StreamToFile ioPair)
        : base()
    {
        _io = ioPair;

        Result<ZLibFormatData> formatValidation =
            _io.Verify()
               .MapResultValueTo((io) => io.Input)
               .OnSuccess(ZLibReader.TryReadHeaderAndFooter)
               .AndEither(successAction: HandleCompletedRead,
                          failureAction: HandleFailedRead);

        ZLibFormatData HandleCompletedRead(ZLibFormatData zlibData)
        {
            // capture input state for debugging
            OperationResult.InputState = new
                (
                    InputFile: null,
                    OutputFile: _io.Output.FullPath,
                    InputIsStream: true,
                    OutputIsStream: false,
                    Header: zlibData.HeaderDisplay,
                    Footer: zlibData.FooterDisplay
                );

            // although enough header and footer bytes were read,
            // fail the overall operation result if the header does not contain a valid zlib signature
            if (!zlibData.HasZLibSignature)
                OperationResult.Fail(ZLibReader.NoZLibSignatureError);

            return zlibData;
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

    /// <inheritdoc/>
    public override async Task<DecompressToFileResult> DecompressAsync()
    {
        if (!OperationResult.Ok)
            return OperationResult;

        await DecompressInputStreamAsync(_io)
              .AndAsyncEither(successAction: SetOperationSuccess,
                              failureAction: SetOperationFailure);

        return OperationResult;
    }

    /// <inheritdoc/>
    public override async Task<DecompressToFileResult> DecompressAsyncWithoutCRC()
    {
        if (!OperationResult.Ok)
            return OperationResult;

        OperationResult.SubLog.Debug(ZLibReader.DecompressStartMsgNoCRC);

        // make a copy of the input stream trimmed of header and footer, leaving only raw deflate data
        DisposableResult<MemoryStream> trimmedInputResult = await _io.Input
                                                                     .TrimToMemoryAsync(ZLibFormatData.ZLibHeaderLength, ZLibFormatData.ZLibFooterLength)
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

    /// <inheritdoc/>
    public override async Task<DecompressToFileResult> DecompressAsyncWithRetry()
    {
        if (!OperationResult.Ok)
            return OperationResult;

        // when executing a retry with streams, we need a copy of the opened input stream,
        // because if the first attempt fails, the input stream will likely be closed by the ZLibStream api,
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

        void LogFirstPassError(IErrorResult error) => OperationResult.SubLog.Error(ZLibReader.DecompressWithRetryError);
    }

    Task<Result<IOutputFile>> DecompressInputStreamAsync(StreamToFile io)
    {
        OperationResult.SubLog.Debug(ZLibReader.DecompressStartMsg);

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
                    return await ZLibAsyncReader.DecompressCoreAsync(ioStreams)
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
                       : Result<IOutputFile>.WithError(Error.DeflateStreamOperationFailed, errorContext: opResult.Error ?? ZLibReader.DeflateFailedError);
            }
        }
        catch (Exception deflateEx)
        {
            return Result<IOutputFile>.WithError(Error.ExceptionWasThrown, deflateEx);
        }
    }
}

