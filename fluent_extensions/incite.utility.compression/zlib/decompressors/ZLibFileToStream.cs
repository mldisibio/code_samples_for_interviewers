using contoso.functional.patterns.result;
using contoso.utility.compression.deflate;
using contoso.utility.compression.entities;

namespace contoso.utility.compression.zlib.decompressors;

/// <summary>Utility to decompress the configured source file to the configured output stream.</summary>
/// <example>
///     <code>
///         var reader = ZLibReader.CreateFor('path_to_input_file').AndExtractToStream(decompressedStream);
///     </code>
/// </example>
internal sealed class ZLibFileToStream : ZInflateToStream
{
    readonly FileToStream _io;

    internal ZLibFileToStream(in FileToStream ioPair)
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
    public override DecompressToStreamResult Decompress()
    {
        if (!OperationResult.Ok)
            return OperationResult;

        DecompressInputFile(_io).AndEither(successAction: SetOperationSuccess,
                                           failureAction: SetOperationFailure);

        return OperationResult;
    }

    /// <inheritdoc/>
    public override DecompressToStreamResult DecompressWithoutCRC()
    {
        if (!OperationResult.Ok)
            return OperationResult;

        OperationResult.SubLog.Debug(ZLibReader.DecompressStartMsgNoCRC);

        // make a copy of the input file trimmed of header and footer, leaving only raw deflate data
        DisposableResult<MemoryStream> trimmedInputResult = _io.Input.TrimToMemory(ZLibFormatData.ZLibHeaderLength, ZLibFormatData.ZLibFooterLength);

        try
        {
            trimmedInputResult.AsResult()
                              .MapResultValueTo((memStream) => new StreamToStream(memStream, _io.Output))
                              .OnSuccess(DeflateTrimmedInput)
                              .OnSuccess(VerifyOutputStream)
                              .AndEither(successAction: SetOperationSuccess,
                                         failureAction: SetOperationFailure);
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
    public override DecompressToStreamResult DecompressWithRetry()
    {
        if (!OperationResult.Ok)
            return OperationResult;

        // retry can be executed without intermediate copies when input is a file because file is opened/closed with each attempt;
        var firstResult = DecompressInputFile(_io).AndEither(successAction: SetOperationSuccess,
                                                             failureAction: HandleFailedFirstPass);

        // if first pass succeeded, we are done; otherwise, invoke the decompression without CRC
        return firstResult.Success ? OperationResult : DecompressWithoutCRC();

        void HandleFailedFirstPass(IErrorResult error)
        {
            // reset output stream position for second attempt
            _io.Output.TryResetPositionToZero();
            OperationResult.SubLog.Error(ZLibReader.DecompressWithRetryError);
        }
    }

    Result<IOutputStream> DecompressInputFile(FileToStream io)
    {
        OperationResult.SubLog.Debug(ZLibReader.DecompressStartMsg);

        return io.Verify()
                 .OnSuccess(InvokeDecompressCore)
                 .OnSuccess(VerifyOutputStream);

        static Result<IOutputStream> InvokeDecompressCore(FileToStream io)
        {
            try
            {
                // open the input file as stream
                using FileStream inputStream = io.Input.OpenRead();
                // wrap the streams
                var ioPair = new StreamToStream(inputStream, io.Output);
                // decompress the input stream to the output stream and return the output file
                return ZLibReader.DecompressCore(ioPair)
                                 .MapResultValueTo((pair) => pair.Output);
            }
            catch (Exception ex)
            {
                return Result<IOutputStream>.WithError(Error.ExceptionWasThrown, ex);
            }
        }
    }

    static Result<IOutputStream> DeflateTrimmedInput(StreamToStream io)
    {
        try
        {
            using var trimmedStream = io.Input.Stream;
            var opResult = DeflateReader.CreateFor(trimmedStream)
                                        .AndExtractToStream(io.Output.Stream)
                                        .Decompress();
            return opResult.Success
                   ? Result<IOutputStream>.WithSuccess(io.Output)
                   : Result<IOutputStream>.WithError(Error.DeflateStreamOperationFailed, errorContext: opResult.Error ?? ZLibReader.DeflateFailedError);
        }
        catch (Exception deflateEx)
        {
            return Result<IOutputStream>.WithError(Error.ExceptionWasThrown, deflateEx);
        }
    }
}

