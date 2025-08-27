using contoso.utility.compression.deflate;
using contoso.functional.patterns.result;
using contoso.utility.compression.entities;

namespace contoso.utility.compression.zlib.decompressors;

/// <summary>Utility to asynchronously decompress the configured source file to the configured output file.</summary>
/// <example>
///     <code>
///         var asyncReader = ZLibReader.CreateFor('path_to_input_file').AndExtractToFileAsync('path_to_output_file');
///     </code>
/// </example>
internal sealed class AsyncZLibFileToFile : ZInflateAsyncToFile
{
    readonly FileToFile _io;

    internal AsyncZLibFileToFile(in FileToFile ioFiles)
        : base()
    {
        _io = ioFiles;

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
                    OutputFile: _io.Output.FullPath,
                    InputIsStream: false,
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
                    InputFile: _io.Input.FullPath,
                    OutputFile: _io.Output.FullPath,
                    InputIsStream: false,
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

        await DecompressInputFileAsync(_io)
              .AndAsyncEither(successAction: SetOperationSuccess,
                              failureAction: SetOperationFailure)
              .ConfigureAwait(false);

        return OperationResult;
    }

    /// <inheritdoc/>
    public override async Task<DecompressToFileResult> DecompressAsyncWithoutCRC()
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

        // retry can be executed without intermediate copies when input is a file because file is opened/closed with each attempt;
        var firstResult = await DecompressInputFileAsync(_io)
                                .AndAsyncEither(successAction: SetOperationSuccess,
                                                failureAction: LogFirstPassError)
                                .ConfigureAwait(false);

        // if first pass succeeded, we are done; otherwise, invoke the decompression without CRC
        return firstResult.Success ? OperationResult : await DecompressAsyncWithoutCRC().ConfigureAwait(false);

        void LogFirstPassError(IErrorResult error) => OperationResult.SubLog.Error(ZLibReader.DecompressWithRetryError);
    }

    async Task<Result<IOutputFile>> DecompressInputFileAsync(FileToFile io)
    {
        OperationResult.SubLog.Debug(ZLibReader.DecompressStartMsg);

        return await io.Verify()
                       .OnSuccessAsync(InvokeDecompressCoreAsync)
                       .OnSuccessAsync(VerifyOutputFile)
                       .ConfigureAwait(false);

        static async Task<Result<IOutputFile>> InvokeDecompressCoreAsync(FileToFile io)
        {
            try
            {
                // open the input file as stream
                FileStream inputStream;
                await using ((inputStream = io.Input.OpenReadForAsync()).ConfigureAwait(false))
                {
                    // create the output file stream
                    FileStream outputStream;
                    await using ((outputStream = io.Output.Create(useAsync: true)).ConfigureAwait(false))
                    {
                        // wrap the streams
                        var ioStreams = new StreamToStream(inputStream, outputStream);
                        // decompress the input stream to the output stream
                        return await ZLibAsyncReader.DecompressCoreAsync(ioStreams)
                                                    .MapResultAsyncTo(_ => io.Output)
                                                    .ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                return Result<IOutputFile>.WithError(Error.ExceptionWasThrown, ex);
            }
        }
    }

    static async Task<Result<IOutputFile>> DeflateTrimmedInputAsync(StreamToFile io)
    {
        try
        {
            Stream trimmedStream;
            await using ((trimmedStream = io.Input.Stream).ConfigureAwait(false))
            {
                var opResult = await DeflateReader.CreateFor(trimmedStream)
                                                  .AndExtractToFileAsync(io.Output.FullPath)
                                                  .DecompressAsync()
                                                  .ConfigureAwait(false);
                return opResult.Success
                       ? Result<IOutputFile>.WithSuccess(io.Output)
                       : Result<IOutputFile>.WithError(Error.DeflateStreamOperationFailed, errorContext: opResult.Error ?? ZLibReader.DeflateFailedError);
            }
        }
        catch (Exception deflateEx)
        {
            return Result<IOutputFile>.WithError(Error.ExceptionWasThrown, deflateEx);
        }
    }
}
