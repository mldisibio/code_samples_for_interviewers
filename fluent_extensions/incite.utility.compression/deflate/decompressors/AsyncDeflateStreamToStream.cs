using contoso.functional.patterns.result;
using contoso.utility.compression.entities;

namespace contoso.utility.compression.deflate.decompressors;

/// <summary>Utility to asynchronously decompress the configured source stream to the configured output stream.</summary>
/// <example>
///     <code>
///         var asyncReader = DeflateReader.CreateFor(deflateStream).AndExtractToFileAsync(decompressedStream);
///     </code>
/// </example>
internal sealed class AsyncDeflateStreamToStream : InflateAsyncToStream
{
    readonly StreamToStream _io;

    internal AsyncDeflateStreamToStream(in StreamToStream ioStreams)
        : base()
    {
        _io = ioStreams;

        _io.Verify()
           .AndEither(successAction: HandleCompletedRead,
                      failureAction: HandleFailedRead);

        // note: we don't explicitly succeed the OperationResult from the constructor, only explicitly fail
        StreamToStream HandleCompletedRead(StreamToStream io)
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
                    OutputIsStream: true,
                    Header: null,
                    Footer: null
                );
        }
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

    Task<Result<IOutputStream>> DecompressInputStreamAsync(StreamToStream io)
    {
        OperationResult.SubLog.Debug(DeflateReader.DecompressStartMsg);

        return io.Verify()
                 .OnSuccessAsync(DeflateAsyncReader.DecompressCoreAsync)
                 .MapResultValueAsyncTo((pair) => pair.Output)
                 .OnSuccessAsync(VerifyOutputStream);
    }
}

