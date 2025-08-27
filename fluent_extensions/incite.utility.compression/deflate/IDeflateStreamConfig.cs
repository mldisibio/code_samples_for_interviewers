
namespace contoso.utility.compression.deflate;

/// <summary>Fluent configuration wrapper.</summary>
public interface IDeflateStreamConfig
{
    /// <summary>
    /// Extract the raw deflate stream to the full path given by <paramref name="outputFileName"/>.
    /// </summary>
    InflateToFile AndExtractToFile(string outputFileName);

    /// <summary>
    /// Asynchronously extract the raw deflate stream to the full path given by <paramref name="outputFileName"/>.
    /// </summary>
    InflateAsyncToFile AndExtractToFileAsync(string outputFileName);

    /// <summary>
    /// Extract the raw deflate stream to the given <paramref name="outputStream"/>.
    /// This is intended to support extracting to a writable stream that supports seeking
    /// <paramref name="outputStream"/> must be already opened and caller is responsible for closing it.
    /// </summary>
    InflateToStream AndExtractToStream(Stream outputStream);

    /// <summary>
    /// Asynchronously extract the raw deflate stream to the given <paramref name="outputStream"/>.
    /// This is intended to support extracting to a writable stream that supports seeking
    /// <paramref name="outputStream"/> must be already opened and caller is responsible for closing it.
    /// </summary>
    InflateAsyncToStream AndExtractToStreamAsync(Stream outputStream);
}
