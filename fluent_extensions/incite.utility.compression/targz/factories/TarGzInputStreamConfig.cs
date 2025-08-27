using contoso.utility.compression.entities;
using contoso.utility.compression.targz.extractors;

namespace contoso.utility.compression.targz.factories;

internal class TarGzInputStreamConfig : ITarGzStreamConfig
{
    readonly IInputStream _inputStream;

    /// <summary>Internally initialized with a zipped tar input stream from <see cref="TarGzReader"/> factory method.</summary>
    internal TarGzInputStreamConfig(in IInputStream inputStream) => _inputStream = inputStream;

    /// <summary>
    /// Unzip and extract the tar archive stream to the directory <paramref name="outputDirectoryPath"/>.
    /// </summary>
    /// <example>
    ///     <code>
    ///         var reader = TarReader.CreateFor(inputStream).AndExtractToDirectory('path_to_output_directory');
    ///     </code>
    /// </example>
    public UnzipAndExtractToDirectory AndExtractToDirectory(string outputDirectoryPath)
    {
        IOutputDirectory outputDirectory = OutputDirectory.CreateFrom(outputDirectoryPath);
        return new TarGzStreamToDirectory(new StreamToDirectory(_inputStream, outputDirectory));
    }

    /// <summary>
    /// Asynchronously unzip and extract the tar archive stream to the directory <paramref name="outputDirectoryPath"/>.
    /// </summary>
    /// <example>
    ///     <code>
    ///         var asyncReader = TarReader.CreateFor(inputStream).AndExtractToDirectoryAsync('path_to_output_directory');
    ///     </code>
    /// </example>
    public UnzipAndExtractAsyncToDirectory AndExtractToDirectoryAsync(string outputDirectoryPath)
    {
        IOutputDirectory outputDirectory = OutputDirectory.CreateFrom(outputDirectoryPath);
        return new AsyncTarGzStreamToDirectory(new StreamToDirectory(_inputStream, outputDirectory));
    }
}
