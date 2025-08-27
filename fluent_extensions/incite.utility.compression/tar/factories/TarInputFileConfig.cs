using contoso.utility.compression.tar.extractors;
using contoso.utility.compression.entities;

namespace contoso.utility.compression.tar.factories;

internal class TarInputFileConfig : ITarFileConfig
{
    readonly IInputFile _inputFile;

    /// <summary>Internally initialized with the input file wrapper from <see cref="TarReader"/> factory method.</summary>
    internal TarInputFileConfig(in IInputFile input) => _inputFile = input;

    /// <summary>
    /// Extract the tar archive to the directory <paramref name="outputDirectoryPath"/>.
    /// </summary>
    /// <example>
    ///     <code>
    ///         var reader = TarReader.CreateFor('path_to_input_file').AndExtractToDirectory('path_to_output_directory');
    ///     </code>
    /// </example>
    public ExtractToDirectory AndExtractToDirectory(string outputDirectoryPath)
    {
        IOutputDirectory outputDirectory = OutputDirectory.CreateFrom(outputDirectoryPath);
        return new TarFileToDirectory(new FileToDirectory(_inputFile, outputDirectory));
    }

    /// <summary>
    /// Asynchronously extract the tar archive to the directory <paramref name="outputDirectoryPath"/>.
    /// </summary>
    /// <example>
    ///     <code>
    ///         var asyncReader = TarReader.CreateFor('path_to_input_file').AndExtractToDirectoryAsync('path_to_output_directory');
    ///     </code>
    /// </example>
    public ExtractAsyncToDirectory AndExtractToDirectoryAsync(string outputDirectoryPath)
    {
        IOutputDirectory outputDirectory = OutputDirectory.CreateFrom(outputDirectoryPath);
        return new AsyncTarFileToDirectory(new FileToDirectory(_inputFile, outputDirectory));
    }
}
