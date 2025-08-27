using contoso.logging.sublog;
using contoso.functional.patterns.result;
using contoso.utility.compression.entities;

namespace contoso.utility.compression.zlib;

/// <summary>Common for all synchronous implementations inflating a zlib file or stream to an output file.</summary>
public abstract class ZInflateToFile
{
    /// <summary>Base ctor.</summary>
    protected ZInflateToFile()
    {
        var subLog = new OperationLog(new AlignedMsgLineFormatter());
        OperationResult = new DecompressToFileResult(subLog);
    }

    /// <summary>Wraps operation state and final success or failure.</summary>
    public DecompressToFileResult OperationResult { get; init; }

    /// <summary>Decompress the configured source to the configured output file.</summary>
    /// <returns>
    /// A <see cref="DecompressToFileResult"/> where 'Success' is:
    /// True if the source is inflated without error and the output file exists on disk. A zero length output returns false because it will be removed.
    /// Otherwise false. Failure reason and/or exception will be found in <see cref="DecompressToStreamResult.Error"/>.
    /// </returns>
    public abstract DecompressToFileResult Decompress();

    /// <summary>
    /// Decompress the configured source to the configured output file
    /// ignoring the zlib header and footer and applying the deflate algorithm to the compressed data only.
    /// </summary>
    /// <returns>
    /// A <see cref="DecompressToFileResult"/> where 'Success' is:
    /// True if the source is inflated without error and the output file exists on disk. A zero length output returns false because it will be removed.
    /// Otherwise false. Failure reason and/or exception will be found in <see cref="DecompressToStreamResult.Error"/>.
    /// </returns>
    public abstract DecompressToFileResult DecompressWithoutCRC();

    /// <summary>
    /// Attempt to decompress the source using the zlib library, and if the operation fails,
    /// attempt a second pass ignoring the zlib header and footer and applying the deflate algorithm to the compressed data only.
    /// </summary>
    /// <returns>
    /// A <see cref="DecompressToFileResult"/> where 'Success' is:
    /// True if the source is inflated without error and the output file exists on disk. A zero length output returns false because it will be removed.
    /// Otherwise false. Failure reason and/or exception will be found in <see cref="DecompressToStreamResult.Error"/>.
    /// </returns>
    public abstract DecompressToFileResult DecompressWithRetry();

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
        OperationResult.OutputFile = output.FullPath;
        OperationResult.SetSuccessful();
        return output;
    }

    /// <summary>Set final operation state to failure and record the error responsible for failure.</summary>
    protected virtual void SetOperationFailure(IErrorResult error) => OperationResult.Fail(error.ErrorMessage);
}

