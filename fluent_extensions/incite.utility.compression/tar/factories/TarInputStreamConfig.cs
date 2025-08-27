using contoso.utility.compression.tar.extractors;
using contoso.utility.compression.entities;

namespace contoso.utility.compression.tar.factories;

internal class TarInputStreamConfig : ITarStreamConfig
{
    readonly IInputStream _inputStream;

    /// <summary>Internally initialized with a tar input stream from <see cref="TarReader"/> factory method.</summary>
    internal TarInputStreamConfig(in IInputStream inputStream) => _inputStream = inputStream;

    /// <summary>
    /// Extract the tar archive stream to the directory <paramref name="outputDirectoryPath"/>.
    /// </summary>
    /// <example>
    ///     <code>
    ///         var reader = TarReader.CreateFor(inputStream).AndExtractToDirectory('path_to_output_directory');
    ///     </code>
    /// </example>
    public ExtractToDirectory AndExtractToDirectory(string outputDirectoryPath)
    {
        IOutputDirectory outputDirectory = OutputDirectory.CreateFrom(outputDirectoryPath);
        return new TarStreamToDirectory(new StreamToDirectory(_inputStream, outputDirectory));
    }

    /// <summary>
    /// Asynchronously extract the tar archive stream to the directory <paramref name="outputDirectoryPath"/>.
    /// </summary>
    /// <example>
    ///     <code>
    ///         var asyncReader = TarReader.CreateFor(inputStream).AndExtractToDirectoryAsync('path_to_output_directory');
    ///     </code>
    /// </example>
    public ExtractAsyncToDirectory AndExtractToDirectoryAsync(string outputDirectoryPath)
    {
        IOutputDirectory outputDirectory = OutputDirectory.CreateFrom(outputDirectoryPath);
        return new AsyncTarStreamToDirectory(new StreamToDirectory(_inputStream, outputDirectory));
    }
}
