using contoso.functional.patterns.result;
using contoso.utility.compression.entities;

namespace contoso.utility.compression.deflate.decompressors;

/// <summary>Utility to decompress the configured source stream to the configured output stream.</summary>
/// <example>
///     <code>
///        DeflateStreamToStream deflateReader = DeflateReader.CreateFor(deflateStream).AndExtractToStream(decompressedStream);
///     </code>
/// </example>
internal sealed class DeflateStreamToStream : InflateToStream
{
    readonly StreamToStream _io;

    internal DeflateStreamToStream(in StreamToStream ioStreams)
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
    public override DecompressToStreamResult Decompress()
    {
        if (!OperationResult.Ok)
            return OperationResult;

        DecompressInputStream(_io)
        .AndEither(successAction: SetOperationSuccess,
                   failureAction: SetOperationFailure);

        return OperationResult;
    }

    Result<IOutputStream> DecompressInputStream(StreamToStream io)
    {
        OperationResult.SubLog.Debug(DeflateReader.DecompressStartMsg);

        return io.Verify()
                 .OnSuccess(DeflateReader.DecompressCore)
                 .MapResultValueTo(pair => pair.Output)
                 .OnSuccess(VerifyOutputStream);
    }
}

