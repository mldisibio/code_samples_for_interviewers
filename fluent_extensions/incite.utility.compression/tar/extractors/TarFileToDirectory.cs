using contoso.functional.patterns.result;
using contoso.utility.compression.entities;
using contoso.utility.compression.tar.entities;

namespace contoso.utility.compression.tar.extractors;

internal sealed class TarFileToDirectory : ExtractToDirectory
{
    readonly FileToDirectory _io;

    internal TarFileToDirectory(in FileToDirectory ioPair)
        : base()
    {
        _io = ioPair;

        _io.Verify()
           .AndEither(successAction: HandleCompletedRead,
                      failureAction: HandleFailedRead);

        // note: we don't explicitly succeed the OperationResult from the constructor, only explicitly fail
        FileToDirectory HandleCompletedRead(FileToDirectory io)
        {
            CaptureState();
            return io;
        }

        void HandleFailedRead(IErrorResult error)
        {
            CaptureState();
            // fail operation with first error
            OperationResult.Fail(error.ErrorMessage);
        }

        void CaptureState()
        {
            // capture input state for debugging
            OperationResult.InputState = new
                (
                    InputFile: _io.Input.FullPath,
                    OutputFile: null,
                    InputIsStream: false,
                    OutputIsStream: false,
                    Header: null,
                    Footer: null
                );
        }
    }

    /// <inheritdoc/>
    public override ExtractToDirectoryResult Extract()
    {
        if (!OperationResult.Ok)
            return OperationResult;

        ExtractArchiveFile(_io).AndEither(successAction: SetOperationSuccess,
                                           failureAction: SetOperationFailure);

        return OperationResult;
    }

    Result<IOutputDirectory> ExtractArchiveFile(FileToDirectory io)
    {
        OperationResult.SubLog.Debug(TarReader.ExtractionStartMsg);

        return io.Verify()
                 .OnSuccess(io => InvokeDecompressCore(io, OperationResult))
                 .OnSuccess(VerifyOutputDirectory);

        static Result<IOutputDirectory> InvokeDecompressCore(FileToDirectory io, ExtractToDirectoryResult opResult)
        {
            try
            {
                // open the input file as stream
                using FileStream inputStream = io.Input.OpenRead();
                // wrap the streams
                var ioPair = new StreamToDirectory(inputStream, io.Output);
                var tarReaderInfo = new TarReaderInfo(ioPair, opResult);
                // decompress the input stream to the output directory and return the output directory
                return TarReader.ExtractCore(tarReaderInfo)
                                .MapResultValueTo(_ => io.Output);
            }
            catch (Exception ex)
            {
                return Result<IOutputDirectory>.WithError(Error.ExceptionWasThrown, ex);
            }
        }
    }
}
