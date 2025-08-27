
namespace contoso.utility.compression.gzip;

/// <summary>Fluent configuration wrapper.</summary>
public interface IGZipStreamConfig
{
    /// <summary>
    /// Extract the zipped stream to the full path given by <paramref name="outputFileName"/>.
    /// </summary>
    UnzipToFile AndExtractToFile(string outputFileName);

    /// <summary>
    /// Asynchronously extract the zipped stream to the full path given by <paramref name="outputFileName"/>.
    /// </summary>
    UnzipAsyncToFile AndExtractToFileAsync(string outputFileName);

    /// <summary>
    /// Extract the zipped stream to the given <paramref name="outputStream"/>.
    /// This is intended to support extracting to a writable stream that supports seeking
    /// <paramref name="outputStream"/> must be already opened and caller is responsible for closing it.
    /// </summary>
    UnzipToStream AndExtractToStream(Stream outputStream);

    /// <summary>
    /// Asynchronously extract the zipped stream to the given <paramref name="outputStream"/>.
    /// This is intended to support extracting to a writable stream that supports seeking
    /// <paramref name="outputStream"/> must be already opened and caller is responsible for closing it.
    /// </summary>
    UnzipAsyncToStream AndExtractToStreamAsync(Stream outputStream);
}
