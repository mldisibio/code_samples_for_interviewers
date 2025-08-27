using contoso.utility.compression.deflate;
using contoso.functional.patterns.result;
using contoso.utility.compression.entities;

namespace contoso.utility.compression.gzip.decompressors;

/// <summary>Utility to decompress the configured source file to the configured output stream.</summary>
/// <example>
///     <code>
///         var zipReader = GzipReader.CreateFor('path_to_input_file').AndExtractToStream(decompressedStream);
///     </code>
/// </example>
internal sealed class GZipFileToStream : UnzipToStream
{
    readonly FileToStream _io;

    internal GZipFileToStream(in FileToStream ioPair)
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
                    InputFile: _io.Input.FullPath,
                    OutputFile: null,
                    InputIsStream: false,
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

        OperationResult.SubLog.Debug(GZipReader.DecompressStartMsgNoCRC);

        // make a copy of the input file trimmed of header and footer, leaving only raw deflate data
        // TODO if the fourth byte is 0x08 (fourth bit is set) then we need to also skip the file name up to and including first 00 terminator;
        //      however, gzip 1.5 on oasis does not seem to do this (new versions might)
        DisposableResult<MemoryStream> trimmedInputResult = _io.Input.TrimToMemory(GZipFormatData.GZipHeaderLength, GZipFormatData.GZipFooterLength);

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
            OperationResult.SubLog.Error(GZipReader.DecompressWithRetryError);
        }

    }

    Result<IOutputStream> DecompressInputFile(FileToStream io)
    {
        OperationResult.SubLog.Debug(GZipReader.DecompressStartMsg);

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
                return GZipReader.DecompressCore(ioPair)
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
                   : Result<IOutputStream>.WithError(Error.DeflateStreamOperationFailed, errorContext: opResult.Error ?? GZipReader.DeflateFailedError);
        }
        catch (Exception deflateEx)
        {
            return Result<IOutputStream>.WithError(Error.ExceptionWasThrown, deflateEx);
        }
    }
}

