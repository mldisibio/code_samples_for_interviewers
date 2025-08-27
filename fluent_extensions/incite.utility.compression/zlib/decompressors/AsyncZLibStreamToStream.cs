using contoso.functional.patterns.result;
using contoso.utility.compression.deflate;
using contoso.utility.compression.entities;

namespace contoso.utility.compression.zlib.decompressors;

/// <summary>Utility to asynchronously decompress the configured source stream to the configured output stream.</summary>
/// <example>
///     <code>
///         var asyncReader = ZLibReader.CreateFor(zlibStream).AndExtractToFileAsync(decompressedStream);
///     </code>
/// </example>
internal sealed class AsyncZLibStreamToStream : ZInflateAsyncToStream
{
    readonly StreamToStream _io;

    internal AsyncZLibStreamToStream(in StreamToStream ioStreams)
        : base()
    {
        _io = ioStreams;

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
                    OutputFile: null,
                    InputIsStream: true,
                    OutputIsStream: true,
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

        OperationResult.SubLog.Debug(ZLibReader.DecompressStartMsgNoCRC);

        // make a copy of the input stream trimmed of header and footer, leaving only raw deflate data
        DisposableResult<MemoryStream> trimmedInputResult = await _io.Input
                                                                     .TrimToMemoryAsync(ZLibFormatData.ZLibHeaderLength, ZLibFormatData.ZLibFooterLength)
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
        // because if the first attempt fails, the input stream will likely be closed by the ZLibStream api,
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
            OperationResult.SubLog.Error(ZLibReader.DecompressWithRetryError);
        }
    }

    Task<Result<IOutputStream>> DecompressInputStreamAsync(StreamToStream io)
    {
        OperationResult.SubLog.Debug(ZLibReader.DecompressStartMsg);

        return io.Verify()
                 .OnSuccessAsync(ZLibAsyncReader.DecompressCoreAsync)
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
                       : Result<IOutputStream>.WithError(Error.DeflateStreamOperationFailed, errorContext: opResult.Error ?? ZLibReader.DeflateFailedError);
            }
        }
        catch (Exception deflateEx)
        {
            return Result<IOutputStream>.WithError(Error.ExceptionWasThrown, deflateEx);
        }
    }
}

