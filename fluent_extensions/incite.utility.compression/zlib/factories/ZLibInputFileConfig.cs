using contoso.utility.compression.entities;
using contoso.utility.compression.zlib.decompressors;

namespace contoso.utility.compression.zlib;

/// <summary>Fluent configuration wrapper.</summary>
internal class ZLibInputFileConfig : IZLibFileConfig
{
    readonly IInputFile _inputFile;

    /// <summary>
    /// Internally initialized with the input file wrapper from <see cref="ZLibReader"/> factory method.
    /// </summary>
    internal ZLibInputFileConfig(in IInputFile input) => _inputFile = input;

    /// <summary>
    /// Extract the zlib file to the full path given by <paramref name="outputFileName"/>.
    /// Use this method when you want to alter the name of the output file from its original.
    /// </summary>
    /// <example>
    ///     <code>
    ///         var reader = ZLibReader.CreateFor('path_to_input_file').AndExtractToFile('path_to_output_file');
    ///     </code>
    /// </example>
    public ZInflateToFile AndExtractToFile(string outputFileName)
    {
        IOutputFile outputFile = OutputFile.CreateOver(outputFileName);
        return new ZLibFileToFile(new FileToFile(_inputFile, outputFile));
    }

    /// <summary>
    /// Asynchronously extract the zlib file to the full path given by <paramref name="outputFileName"/>.
    /// Use this method when you want to alter the name of the output file from its original.
    /// </summary>
    /// <example>
    ///     <code>
    ///         var asyncReader = ZLibReader.CreateFor('path_to_input_file').AndExtractToFileAsync('path_to_output_file');
    ///     </code>
    /// </example>
    public ZInflateAsyncToFile AndExtractToFileAsync(string outputFileName)
    {
        IOutputFile outputFile = OutputFile.CreateOver(outputFileName);
        return new AsyncZLibFileToFile(new FileToFile(_inputFile, outputFile));
    }

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
    /// <example>
    ///     <code>
    ///         var reader = ZLibReader.CreateFor('path_to_input_file').AndExtractToDirectory('path_to_output_dir');
    ///     </code>
    /// </example>
    public ZInflateToFile AndExtractToDirectory(string outputDirectory, bool removeExtension = true)
    {
        IOutputFile outputFile = OutputFile.CreateFrom(outputDirectory, _inputFile, removeExtension);
        return new ZLibFileToFile(new FileToFile(_inputFile, outputFile));
    }

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
    /// <example>
    ///     <code>
    ///         var asyncReader = ZLibReader.CreateFor('path_to_input_file').AndExtractToDirectoryAsync('path_to_output_dir');
    ///     </code>
    /// </example>
    public ZInflateAsyncToFile AndExtractToDirectoryAsync(string outputDirectory, bool removeExtension = true)
    {
        IOutputFile outputFile = OutputFile.CreateFrom(outputDirectory, _inputFile, removeExtension);
        return new AsyncZLibFileToFile(new FileToFile(_inputFile, outputFile));
    }

    /// <summary>
    /// Extract the zlib stream to the given <paramref name="outputStream"/>.
    /// This is intended to support extracting to a writable stream that supports seeking
    /// <paramref name="outputStream"/> must be already opened and caller is responsible for closing it.
    /// </summary>
    /// <example>
    ///     <code>
    ///         var reader = ZLibReader.CreateFor('path_to_input_file').AndExtractToStream(outputStream);
    ///     </code>
    /// </example>
    public ZInflateToStream AndExtractToStream(Stream outputStream)
    {
        return new ZLibFileToStream(new FileToStream(_inputFile, OutputStream.CreateOver(outputStream)));
    }

    /// <summary>
    /// Asynchronously extract the zlib stream to the given <paramref name="outputStream"/>.
    /// This is intended to support extracting to a writable stream that supports seeking
    /// <paramref name="outputStream"/> must be already opened and caller is responsible for closing it.
    /// </summary>
    /// <example>
    ///     <code>
    ///         var asyncReader = ZLibReader.CreateFor('path_to_input_file').AndExtractToStreamAsync(outputStream);
    ///     </code>
    /// </example>
    public ZInflateAsyncToStream AndExtractToStreamAsync(Stream outputStream)
    {
        return new AsyncZLibFileToStream(new FileToStream(_inputFile, OutputStream.CreateOver(outputStream)));
    }
}
