using contoso.functional.patterns.result;
using contoso.utility.compression.deflate;
using contoso.utility.compression.entities;

namespace contoso.utility.compression.zlib.decompressors;

/// <summary>Utility to asynchronously decompress the configured source file to the configured output stream.</summary>
/// <example>
///     <code>
///         var asyncReader = ZLibReader.CreateFor('path_to_input_file').AndExtractToStreamAsync(decompressedStream);
///     </code>
/// </example>
internal sealed class AsyncZLibFileToStream : ZInflateAsyncToStream
{
    readonly FileToStream _io;

    internal AsyncZLibFileToStream(in FileToStream ioPair)
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
                    InputFile: _io.Input.FullPath,
                    OutputFile: null,
                    InputIsStream: false,
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
                    InputFile: _io.Input.FullPath,
                    OutputFile: null,
                    InputIsStream: false,
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

        await DecompressInputFileAsync(_io)
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

        // make a copy of the input file trimmed of header and footer, leaving only raw deflate data
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

        // retry can be executed without intermediate copies when input is a file because file is opened/closed with each attempt;
        var firstResult =await DecompressInputFileAsync(_io)
                               .AndAsyncEither(successAction: SetOperationSuccess,
                                               failureAction: HandleFailedFirstPass)
                               .ConfigureAwait(false);

        // if first pass succeeded, we are done; otherwise, invoke the decompression without CRC
        return firstResult.Success ? OperationResult : await DecompressAsyncWithoutCRC().ConfigureAwait(false);


        void HandleFailedFirstPass(IErrorResult error)
        {
            // reset output stream position for second attempt
            _io.Output.TryResetPositionToZero();
            OperationResult.SubLog.Error(ZLibReader.DecompressWithRetryError);
        }
    }

    Task<Result<IOutputStream>> DecompressInputFileAsync(FileToStream io)
    {
        OperationResult.SubLog.Debug(ZLibReader.DecompressStartMsg);

        return io.Verify()
                 .OnSuccessAsync(InvokeDecompressCoreAsync)
                 .OnSuccessAsync(VerifyOutputStream);

        static async Task<Result<IOutputStream>> InvokeDecompressCoreAsync(FileToStream io)
        {
            try
            {
                // open the input file as stream
                FileStream inputStream;
                await using ((inputStream = io.Input.OpenReadForAsync()).ConfigureAwait(false))
                {
                    // wrap the streams
                    var ioStreams = new StreamToStream(inputStream, io.Output);
                    // decompress the input stream to the output stream and return the output file
                    return await ZLibAsyncReader.DecompressCoreAsync(ioStreams)
                                                .MapResultValueAsyncTo((pair) => pair.Output)
                                                .ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                return Result<IOutputStream>.WithError(Error.ExceptionWasThrown, ex);
            }
        }
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

