using contoso.logging.sublog;
using contoso.functional.patterns.result;
using contoso.utility.compression.entities;

namespace contoso.utility.compression.tar;

/// <summary>Common for all asynchronous implementations extracting a tar archive to an output directory.</summary>
public abstract class ExtractAsyncToDirectory
{
    /// <summary>Base ctor.</summary>
    protected ExtractAsyncToDirectory()
    {
        var subLog = new OperationLog(new AlignedMsgLineFormatter());
        OperationResult = new ExtractToDirectoryResult(subLog);
    }

    /// <summary>Wraps operation state and final success or failure.</summary>
    public ExtractToDirectoryResult OperationResult { get; init; }

    /// <summary>Asynchronously extract a tar archive into the configured output directory.</summary>
    /// <returns>
    /// A <see cref="ExtractToDirectoryResult"/> where 'Success' is:
    /// True if any of the archived files were extracted and the output directory is accessible and has content.
    /// Otherwise false. Failure reason and/or exception will be found in <see cref="compression.DecompressToStreamResult.Error"/>.
    /// </returns>
    public abstract Task<ExtractToDirectoryResult> ExtractAsync();

    /// <summary>
    /// Verify the directory to which the tar archives were extracted still exists on disk
    /// and has at least some non-zero-length files
    /// </summary>
    protected Result<IOutputDirectory> VerifyOutputDirectory(IOutputDirectory output)
    {
        // check each file path exists or remove from list
        OperationResult.ValidateExtractedFiles();
        int expectedCount = OperationResult.ExpectedFileCount;

        // check if output directory still has files;
        // if not, remove directory and return failure;
        // if any, log whether the actual count matches the expected count
        //         but return success as long as any files were extracted
        return output.GetFileCount()
                     .AndEither(successAction: OnDirectoryHasContent,
                                failureAction: err => TryCleanupEmptyOutput(err, output));

        IOutputDirectory OnDirectoryHasContent(int actualCount)
        {
            OperationResult.SubLog.Debug($"Expected {OperationResult.ExpectedFileCount:N0} files. Extracted {actualCount:N0}.");
            return output;
        }
    }

    /// <summary>Attempt to remove any empty output directory.</summary>
    protected virtual void TryCleanupEmptyOutput(IErrorResult error, IOutputDirectory output)
    {
        if (error.Error == Error.OutputDirectoryIsEmpty)
        {
            OperationResult.SubLog.Error("Output directory is empty and will be removed");
            var cleanupResult = output.TryRemove();
            if (!cleanupResult.Success)
                OperationResult.SubLog.Error(cleanupResult.ErrorMessage);
        }
    }

    /// <summary>Set final operation state to success and record the path to the final output directory.</summary>
    protected virtual IOutputDirectory SetOperationSuccess(IOutputDirectory output)
    {
        OperationResult.OutputDirectory = output.FullPath;
        OperationResult.SetSuccessful();
        return output;
    }

    /// <summary>Set final operation state to failure and record the error responsible for failure.</summary>
    protected virtual void SetOperationFailure(IErrorResult error) => OperationResult.Fail(error.ErrorMessage);

}
