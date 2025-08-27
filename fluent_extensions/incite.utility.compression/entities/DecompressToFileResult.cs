using contoso.logging.sublog;

namespace contoso.utility.compression;

/// <summary>Wraps operation state and final success or failure when decompressing to a file.</summary>
public class DecompressToFileResult : DecompressToStreamResult
{
    internal DecompressToFileResult(IOperationLog<IMsgLine> subLog) : base(subLog) { }

    /// <summary>Full path to decompressed output file on disk.</summary>
    public string? OutputFile { get; internal set; }

}
