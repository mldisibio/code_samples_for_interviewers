using contoso.logging.sublog;
using contoso.functional.patterns.result;
using contoso.utility.compression.entities;

namespace contoso.utility.compression.deflate;

/// <summary>Common for all asynchronous implementations decompressing a raw deflate stream to an output stream.</summary>
public abstract class InflateAsyncToStream
{
    /// <summary>Base ctor.</summary>
    protected InflateAsyncToStream()
    {
        var subLog = new OperationLog(new AlignedMsgLineFormatter());
        OperationResult = new DecompressToStreamResult(subLog);
    }

    /// <summary>Wraps operation state and final success or failure.</summary>
    public DecompressToStreamResult OperationResult { get; init; }

    /// <summary>Asynchronously decompress the configured source to the configured output stream.</summary>
    /// <returns>
    /// A <see cref="DecompressToStreamResult"/> where 'Success' is:
    /// True if the source is inflated without error and the output stream is accessible and has content.
    /// Otherwise false. Failure reason and/or exception will be found in <see cref="DecompressToStreamResult.Error"/>.
    /// </returns>
    public abstract Task<DecompressToStreamResult> DecompressAsync();

    /// <summary>Verify the output stream is still accessible and has content.</summary>
    protected virtual Result<IOutputStream> VerifyOutputStream(IOutputStream output)
    {
        return output.VerifyLength()
                     .AndEither(successAction: OnStreamHasContent,
                                failureAction: LogFailure);

        IOutputStream OnStreamHasContent(long length)
        {
            OperationResult.SubLog.Debug($"Extracted {length:N0} [{length.ToFormattedSizeDisplay()}] to stream");
            return output;
        }

        void LogFailure(IErrorResult error) => OperationResult.SubLog.Error(error.ErrorMessage);
    }

    /// <summary>Set final operation state to success.</summary>
    protected virtual IOutputStream SetOperationSuccess(IOutputStream output)
    {
        // set final state of OperationResult
        OperationResult.SetSuccessful();
        return output;
    }

    /// <summary>Set opertion to failed state and record the error responsible for failure.</summary>
    protected virtual void SetOperationFailure(IErrorResult error) => OperationResult.Fail(error.ErrorMessage);

}
