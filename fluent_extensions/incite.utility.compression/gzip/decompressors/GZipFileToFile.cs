using contoso.utility.compression.deflate;
using contoso.functional.patterns.result;
using contoso.utility.compression.entities;

namespace contoso.utility.compression.gzip.decompressors;

/// <summary>Utility to decompress the configured source file to the configured output file.</summary>
/// <example>
///     <code>
///         var zipReader = GzipReader.CreateFor('path_to_input_file').AndExtractToFile('path_to_output_file');
///     </code>
/// </example>
internal sealed class GZipFileToFile : UnzipToFile
{
    readonly FileToFile _io;

    internal GZipFileToFile(in FileToFile ioFiles)
        : base()
    {
        _io = ioFiles;

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
                    OutputFile: _io.Output.FullPath,
                    InputIsStream: false,
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
    public override DecompressToFileResult Decompress()
    {
        if (!OperationResult.Ok)
            return OperationResult;

        DecompressInputFile(_io).AndEither(successAction: SetOperationSuccess,
                                           failureAction: SetOperationFailure);

        return OperationResult;
    }

    /// <inheritdoc/>
    public override DecompressToFileResult DecompressWithoutCRC()
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
                              .MapResultValueTo((memStream) => new StreamToFile(memStream, _io.Output))
                              .OnSuccess(DeflateTrimmedInput)
                              .OnSuccess(VerifyOutputFile)
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
    public override DecompressToFileResult DecompressWithRetry()
    {
        if (!OperationResult.Ok)
            return OperationResult;

        // retry can be executed without intermediate copies when input is a file because file is opened/closed with each attempt;
        var firstResult = DecompressInputFile(_io).AndEither(successAction: SetOperationSuccess,
                                                             failureAction: LogFirstPassError);

        // if first pass succeeded, we are done; otherwise, invoke the decompression without CRC
        return firstResult.Success ? OperationResult : DecompressWithoutCRC();

        void LogFirstPassError(IErrorResult error) => OperationResult.SubLog.Error(GZipReader.DecompressWithRetryError);
    }

    Result<IOutputFile> DecompressInputFile(FileToFile io)
    {
        OperationResult.SubLog.Debug(GZipReader.DecompressStartMsg);

        return io.Verify()
                 .OnSuccess(InvokeDecompressCore)
                 .OnSuccess(VerifyOutputFile);

        static Result<IOutputFile> InvokeDecompressCore(FileToFile io)
        {
            try
            {
                // open the input file as stream
                using FileStream inputStream = io.Input.OpenRead();
                // create the output file stream
                using FileStream outputStream = io.Output.Create();
                // wrap the streams
                var ioPair = new StreamToStream(inputStream, outputStream);
                // decompress the input stream to the output stream and return the output file
                return GZipReader.DecompressCore(ioPair)
                                 .MapResultValueTo(_ => io.Output);
            }
            catch (Exception ex)
            {
                return Result<IOutputFile>.WithError(Error.ExceptionWasThrown, ex);
            }
        }
    }

    static Result<IOutputFile> DeflateTrimmedInput(StreamToFile io)
    {
        try
        {
            using var trimmedStream = io.Input.Stream;
            var opResult = DeflateReader.CreateFor(trimmedStream)
                                        .AndExtractToFile(io.Output.FullPath)
                                        .Decompress();
            return opResult.Success
                   ? Result<IOutputFile>.WithSuccess(io.Output)
                   : Result<IOutputFile>.WithError(Error.DeflateStreamOperationFailed, errorContext: opResult.Error ?? GZipReader.DeflateFailedError);
        }
        catch (Exception deflateEx)
        {
            return Result<IOutputFile>.WithError(Error.ExceptionWasThrown, deflateEx);
        }
    }
}

