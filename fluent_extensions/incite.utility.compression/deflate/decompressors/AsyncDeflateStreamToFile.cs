using contoso.functional.patterns.result;
using contoso.utility.compression.entities;

namespace contoso.utility.compression.deflate.decompressors;

/// <summary>Utility to asynchronously decompress the configured source stream to the configured output file.</summary>
/// <example>
///     <code>
///         var asyncReader = DeflateReader.CreateFor(deflateStream).AndExtractToFileAsync('path_to_output_file');
///     </code>
/// </example>
internal sealed class AsyncDeflateStreamToFile : InflateAsyncToFile
{
    readonly StreamToFile _io;

    internal AsyncDeflateStreamToFile(in StreamToFile ioPair)
        : base()
    {
        _io = ioPair;

        _io.Verify()
           .AndEither(successAction: HandleCompletedRead,
                      failureAction: HandleFailedRead);

        // note: we don't explicitly succeed the OperationResult from the constructor, only explicitly fail
        StreamToFile HandleCompletedRead(StreamToFile io)
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
                    OutputFile: _io.Output.FullPath,
                    InputIsStream: true,
                    OutputIsStream: false,
                    Header: null,
                    Footer: null
                );
        }
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

    Task<Result<IOutputFile>> DecompressInputStreamAsync(StreamToFile io)
    {
        OperationResult.SubLog.Debug(DeflateReader.DecompressStartMsg);

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
                    return await DeflateAsyncReader.DecompressCoreAsync(ioStreams)
                                                   .MapResultAsyncTo(_ => io.Output);
                }
            }
            catch (Exception ex)
            {
                return Result<IOutputFile>.WithError(Error.ExceptionWasThrown, ex);
            }
        }
    }
}

