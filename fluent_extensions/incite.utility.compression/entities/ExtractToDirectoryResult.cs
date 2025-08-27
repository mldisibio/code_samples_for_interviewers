using contoso.logging.sublog;

namespace contoso.utility.compression;

/// <summary>Wraps operation state and final success or failure for tar archive extraction.</summary>
public class ExtractToDirectoryResult : DecompressToStreamResult
{
    readonly List<string> _extractedFiles;

    internal ExtractToDirectoryResult(IOperationLog<IMsgLine> subLog) : base(subLog) 
    {
        _extractedFiles = new List<string>(256);
    }

    /// <summary>Full path to decompressed output directory on disk.</summary>
    public string? OutputDirectory { get; internal set; }

    /// <summary>Count of header with a valid file entry.</summary>
    public int ExpectedFileCount { get; private set; }

    /// <summary>Collection of file paths that were successfully extracted.</summary>
    public IReadOnlyCollection<string> ExtractedFiles => _extractedFiles;

    internal void IncrementExpectedFileCount() => ExpectedFileCount++;

    internal void AddExtractedFile(string extractedPath) => _extractedFiles.Add(extractedPath);

    internal void ValidateExtractedFiles()
    {
        var copyOfFiles = _extractedFiles.ToArray();
        for (int i = 0; i < copyOfFiles.Length; i++)
        {
            try
            {
                var file = new FileInfo(copyOfFiles[i]);
                if (file.Exists)
                {
                    if (file.Length == 0)
                    {
                        file.Delete();
                        _extractedFiles.Remove(copyOfFiles[i]);
                    }
                }
                else
                    _extractedFiles.Remove(copyOfFiles[i]);
            }
            catch { }
        }
    }

    List<tar.TarHeaderBlock>? _headers;
    internal void DebugHeader(tar.TarHeaderBlock header) => (_headers ??= new List<tar.TarHeaderBlock>(64)).Add(header);
    /// <summary>Collection of parsed headers for debugging only.</summary>
    public IReadOnlyCollection<tar.TarHeaderBlock> HeaderDiagnostics => _headers ?? new List<tar.TarHeaderBlock>(0);
}
