using contoso.functional.patterns.result;
using contoso.utility.compression.entities;
using contoso.utility.compression.tar.entities;
using contoso.utility.compression.tar.factories;

namespace contoso.utility.compression.tar.extractors;

internal sealed class AsyncTarStreamToDirectory : ExtractAsyncToDirectory
{
    readonly StreamToDirectory _io;

    internal AsyncTarStreamToDirectory(in StreamToDirectory ioPair)
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
    public override async Task<ExtractToDirectoryResult> ExtractAsync()
    {
        if (!OperationResult.Ok)
            return OperationResult;

        await ExtractArchiveFileAsync(_io).AndAsyncEither(successAction: SetOperationSuccess,
                                                          failureAction: SetOperationFailure)
                                          .ConfigureAwait(false);

        return OperationResult;
    }

    Task<Result<IOutputDirectory>> ExtractArchiveFileAsync(StreamToDirectory io)
    {
        OperationResult.SubLog.Debug(TarReader.ExtractionStartMsg);

        return io.Verify()
                 .OnSuccessAsync(io => InvokeDecompressCoreAsync(io, OperationResult))
                 .OnSuccessAsync(VerifyOutputDirectory);

        static async Task<Result<IOutputDirectory>> InvokeDecompressCoreAsync(StreamToDirectory io, ExtractToDirectoryResult opResult)
        {
            try
            {
                var tarReaderInfo = new TarReaderInfo(io, opResult);
                // decompress the input stream to the output directory and return the output directory
                return await TarAsyncReader.ExtractCoreAsync(tarReaderInfo)
                                           .MapResultAsyncTo(_ => io.Output)
                                           .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return Result<IOutputDirectory>.WithError(Error.ExceptionWasThrown, ex);
            }
        }
    }
}
