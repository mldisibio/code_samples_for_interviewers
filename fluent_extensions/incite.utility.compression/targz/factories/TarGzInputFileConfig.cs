using contoso.utility.compression.targz.extractors;
using contoso.utility.compression.entities;

namespace contoso.utility.compression.targz.factories;

internal class TarGzInputFileConfig : ITarGzFileConfig
{
    readonly IInputFile _inputFile;

    /// <summary>Internally initialized with the input file wrapper from <see cref="TarGzReader"/> factory method.</summary>
    internal TarGzInputFileConfig(in IInputFile input) => _inputFile = input;

    /// <summary>
    /// Unzip and extract the tar archive to the directory <paramref name="outputDirectoryPath"/>.
    /// </summary>
    /// <example>
    ///     <code>
    ///         var reader = TarReader.CreateFor('path_to_input_file').AndExtractToDirectory('path_to_output_directory');
    ///     </code>
    /// </example>
    public UnzipAndExtractToDirectory AndExtractToDirectory(string outputDirectoryPath)
    {
        IOutputDirectory outputDirectory = OutputDirectory.CreateFrom(outputDirectoryPath);
        return new TarGzFileToDirectory(new FileToDirectory(_inputFile, outputDirectory));
    }

    /// <summary>
    /// Asynchronously unzip and extract the tar archive to the directory <paramref name="outputDirectoryPath"/>.
    /// </summary>
    /// <example>
    ///     <code>
    ///         var asyncReader = TarReader.CreateFor('path_to_input_file').AndExtractToDirectoryAsync('path_to_output_directory');
    ///     </code>
    /// </example>
    public UnzipAndExtractAsyncToDirectory AndExtractToDirectoryAsync(string outputDirectoryPath)
    {
        IOutputDirectory outputDirectory = OutputDirectory.CreateFrom(outputDirectoryPath);
        return new AsyncTarGzFileToDirectory(new FileToDirectory(_inputFile, outputDirectory));
    }
}
