using contoso.functional.patterns.result;
using contoso.utility.compression.entities;
using contoso.utility.compression.tar.entities;

namespace contoso.utility.compression.tar.extractors;

internal sealed class TarStreamToDirectory : ExtractToDirectory
{
    readonly StreamToDirectory _io;

    internal TarStreamToDirectory(in StreamToDirectory ioPair)
        : base()
    {
        _io = ioPair;

        _io.Verify()
           .AndEither(successAction: HandleCompletedRead,
                      failureAction: HandleFailedRead);

        // note: we don't explicitly succeed the OperationResult from the constructor, only explicitly fail
        StreamToDirectory HandleCompletedRead(StreamToDirectory io)
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
                    InputFile: null,
                    OutputFile: null,
                    InputIsStream: true,
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

    Result<IOutputDirectory> ExtractArchiveFile(StreamToDirectory io)
    {
        OperationResult.SubLog.Debug(TarReader.ExtractionStartMsg);

        return io.Verify()
                 .OnSuccess(io => InvokeDecompressCore(io, OperationResult))
                 .OnSuccess(VerifyOutputDirectory);

        static Result<IOutputDirectory> InvokeDecompressCore(StreamToDirectory io, ExtractToDirectoryResult opResult)
        {
            try
            {
                var tarReaderInfo = new TarReaderInfo(io, opResult);
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
