
namespace contoso.utility.compression.zlib;

/// <summary>Fluent configuration wrapper.</summary>
public interface IZLibStreamConfig
{
    /// <summary>
    /// Extract the zlib stream to the full path given by <paramref name="outputFileName"/>.
    /// </summary>
    ZInflateToFile AndExtractToFile(string outputFileName);

    /// <summary>
    /// Asynchronously extract the zlib stream to the full path given by <paramref name="outputFileName"/>.
    /// </summary>
    ZInflateAsyncToFile AndExtractToFileAsync(string outputFileName);

    /// <summary>
    /// Extract the zlib stream to the given <paramref name="outputStream"/>.
    /// This is intended to support extracting to a writable stream that supports seeking
    /// <paramref name="outputStream"/> must be already opened and caller is responsible for closing it.
    /// </summary>
    ZInflateToStream AndExtractToStream(Stream outputStream);

    /// <summary>
    /// Asynchronously extract the zlib stream to the given <paramref name="outputStream"/>.
    /// This is intended to support extracting to a writable stream that supports seeking
    /// <paramref name="outputStream"/> must be already opened and caller is responsible for closing it.
    /// </summary>
    ZInflateAsyncToStream AndExtractToStreamAsync(Stream outputStream);
}
