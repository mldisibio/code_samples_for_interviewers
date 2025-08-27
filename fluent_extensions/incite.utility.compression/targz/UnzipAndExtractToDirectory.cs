using contoso.logging.sublog;

namespace contoso.utility.compression.targz;

/// <summary>Common for all synchronous implementations decompressing a zipped tar archive to an output directory.</summary>
public abstract class UnzipAndExtractToDirectory
{
    /// <summary>Base ctor.</summary>
    protected UnzipAndExtractToDirectory()
    {
        var subLog = new OperationLog(new AlignedMsgLineFormatter());
        OperationResult = new ExtractToDirectoryResult(subLog);
    }

    /// <summary>Wraps operation state and final success or failure.</summary>
    public ExtractToDirectoryResult OperationResult { get; init; }

    /// <summary>Unzip and extract a tar archive into the configured output directory.</summary>
    /// <returns>
    /// A <see cref="ExtractToDirectoryResult"/> where 'Success' is:
    /// True if any of the archived files were extracted and the output directory is accessible and has content.
    /// Otherwise false. Failure reason and/or exception will be found in <see cref="compression.DecompressToStreamResult.Error"/>.
    /// </returns>
    public abstract ExtractToDirectoryResult UnzipAndExtract();
}
