using contoso.logging.sublog;
using contoso.functional.patterns.result;
using contoso.utility.compression.entities;

namespace contoso.utility.compression.deflate;

/// <summary>Common for all asynchronous implementations decompressing a raw deflate stream to an output file.</summary>
public abstract class InflateAsyncToFile
{
    /// <summary>Base ctor.</summary>
    protected InflateAsyncToFile()
    {
        var subLog = new OperationLog(new AlignedMsgLineFormatter());
        OperationResult = new DecompressToFileResult(subLog);
    }

    /// <summary>Wraps operation state and final success or failure.</summary>
    public DecompressToFileResult OperationResult { get; init; }

    /// <summary>Asynchronously decompress the configured source to the configured output file.</summary>
    /// <returns>
    /// A <see cref="DecompressToFileResult"/> where 'Success' is:
    /// True if the source is inflated without error and the output file exists on disk. A zero length output returns false because it will be removed.
    /// Otherwise false. Failure reason and/or exception will be found in <see cref="DecompressToStreamResult.Error"/>.
    /// </returns>
    public abstract Task<DecompressToFileResult> DecompressAsync();

    /// <summary>Verify the output file exists on disk and has content.</summary>
    protected virtual Result<IOutputFile> VerifyOutputFile(IOutputFile output)
    {
        return output.VerifyLength()
                     .AndEither(successAction: OnFileHasContent,
                                failureAction: (err) => TryCleanupFailedOutput(err, output));

        IOutputFile OnFileHasContent(long length)
        {
            OperationResult.SubLog.Debug($"Extracted {length:N0} [{length.ToFormattedSizeDisplay()}] to file");
            return output;
        }
    }

    /// <summary>Attempt to remove any empty or partially created output file.</summary>
    protected virtual void TryCleanupFailedOutput(IErrorResult error, IOutputFile output)
    {
        // log the case where an output was created but is zero length
        if (error.Error == Error.OutputFileIsEmpty)
            OperationResult.SubLog.Error("Output is zero-length and will be removed");
        // the decompress operation failed;
        // try to cleanup whether or not the reason was a zero-length output
        var cleanupResult = output.TryRemove();
        if (!cleanupResult.Success)
            OperationResult.SubLog.Error(cleanupResult.ErrorMessage);
    }

    /// <summary>Set final operation state to success and record the path to the final output file.</summary>
    protected virtual IOutputFile SetOperationSuccess(IOutputFile output)
    {
        // set final state of OperationResult
        OperationResult.OutputFile = output.FullPath;
        OperationResult.SetSuccessful();
        return output;
    }

    /// <summary>Set final operation state to failure and record the error responsible for failure.</summary>
    protected virtual void SetOperationFailure(IErrorResult error) => OperationResult.Fail(error.ErrorMessage);
}
