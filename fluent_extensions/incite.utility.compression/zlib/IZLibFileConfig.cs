
namespace contoso.utility.compression.zlib;

/// <summary>Fluent configuration wrapper.</summary>
public interface IZLibFileConfig
{
    /// <summary>
    /// Extract the zlib file to the full path given by <paramref name="outputFileName"/>.
    /// Use this method when you want to alter the name of the output file from its original.
    /// </summary>
    ZInflateToFile AndExtractToFile(string outputFileName);

    /// <summary>
    /// Asynchronously extract the zlib file to the full path given by <paramref name="outputFileName"/>.
    /// Use this method when you want to alter the name of the output file from its original.
    /// </summary>
    ZInflateAsyncToFile AndExtractToFileAsync(string outputFileName);

    /// <summary>
    /// Extract the zlib file to the <paramref name="outputDirectory"/> path.
    /// The output file will retain the same name as the input,
    /// with the extension (presumeably '.gz') removed unless specified otherwise.
    /// </summary>
    /// <param name="outputDirectory">Full path the the output directory for the decompression operation.</param>
    /// <param name="removeExtension">
    /// True to remove the extension (presumeably '.gz') from the output file.
    /// False to leave the file with the same name and extension. Default is true.
    /// </param>
    ZInflateToFile AndExtractToDirectory(string outputDirectory, bool removeExtension = true);

    /// <summary>
    /// Asynchronously extract the zlib file to the <paramref name="outputDirectory"/> path.
    /// The output file will retain the same name as the input,
    /// with the extension (presumeably '.gz') removed unless specified otherwise.
    /// </summary>
    /// <param name="outputDirectory">Full path the the output directory for the decompression operation.</param>
    /// <param name="removeExtension">
    /// True to remove the extension (presumeably '.gz') from the output file.
    /// False to leave the file with the same name and extension. Default is true.
    /// </param>
    ZInflateAsyncToFile AndExtractToDirectoryAsync(string outputDirectory, bool removeExtension = true);

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
