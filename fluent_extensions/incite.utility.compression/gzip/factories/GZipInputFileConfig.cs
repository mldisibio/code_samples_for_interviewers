using contoso.utility.compression.entities;
using contoso.utility.compression.gzip.decompressors;

namespace contoso.utility.compression.gzip;

/// <summary>Fluent configuration wrapper.</summary>
internal class GZipInputFileConfig : IGZipFileConfig
{
    readonly IInputFile _inputFile;

    /// <summary>
    /// Internally initialized with the input file wrapper from <see cref="GZipReader"/> factory method.
    /// </summary>
    internal GZipInputFileConfig(in IInputFile input) => _inputFile = input;

    /// <summary>
    /// Extract the zipped file to the full path given by <paramref name="outputFileName"/>.
    /// Use this method when you want to alter the name of the output file from its original.
    /// </summary>
    /// <example>
    ///     <code>
    ///         var zipReader = GzipReader.CreateFor('path_to_input_file').AndExtractToFile('path_to_output_file');
    ///     </code>
    /// </example>
    public UnzipToFile AndExtractToFile(string outputFileName)
    {
        IOutputFile outputFile = OutputFile.CreateOver(outputFileName);
        return new GZipFileToFile(new FileToFile(_inputFile, outputFile));
    }

    /// <summary>
    /// Asynchronously extract the zipped file to the full path given by <paramref name="outputFileName"/>.
    /// Use this method when you want to alter the name of the output file from its original.
    /// </summary>
    /// <example>
    ///     <code>
    ///         var asyncReader = GzipReader.CreateFor('path_to_input_file').AndExtractToFileAsync('path_to_output_file');
    ///     </code>
    /// </example>
    public UnzipAsyncToFile AndExtractToFileAsync(string outputFileName)
    {
        IOutputFile outputFile = OutputFile.CreateOver(outputFileName);
        return new AsyncGZipFileToFile(new FileToFile(_inputFile, outputFile));
    }

    /// <summary>
    /// Extract the zipped file to the <paramref name="outputDirectory"/> path.
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
    ///         var zipReader = GzipReader.CreateFor('path_to_input_file').AndExtractToDirectory('path_to_output_dir');
    ///     </code>
    /// </example>
    public UnzipToFile AndExtractToDirectory(string outputDirectory, bool removeExtension = true)
    {
        IOutputFile outputFile = OutputFile.CreateFrom(outputDirectory, _inputFile, removeExtension);
        return new GZipFileToFile(new FileToFile(_inputFile, outputFile));
    }

    /// <summary>
    /// Asynchronously extract the zipped file to the <paramref name="outputDirectory"/> path.
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
    ///         var asyncReader = GzipReader.CreateFor('path_to_input_file').AndExtractToDirectoryAsync('path_to_output_dir');
    ///     </code>
    /// </example>
    public UnzipAsyncToFile AndExtractToDirectoryAsync(string outputDirectory, bool removeExtension = true)
    {
        IOutputFile outputFile = OutputFile.CreateFrom(outputDirectory, _inputFile, removeExtension);
        return new AsyncGZipFileToFile(new FileToFile(_inputFile, outputFile));
    }

    /// <summary>
    /// Extract the zipped stream to the given <paramref name="outputStream"/>.
    /// This is intended to support extracting to a writable stream that supports seeking
    /// <paramref name="outputStream"/> must be already opened and caller is responsible for closing it.
    /// </summary>
    /// <example>
    ///     <code>
    ///         var zipReader = GzipReader.CreateFor('path_to_input_file').AndExtractToStream(decompressedStream);
    ///     </code>
    /// </example>
    public UnzipToStream AndExtractToStream(Stream outputStream)
    {
        return new GZipFileToStream(new FileToStream(_inputFile, OutputStream.CreateOver(outputStream)));
    }

    /// <summary>
    /// Asynchronously extract the zipped stream to the given <paramref name="outputStream"/>.
    /// This is intended to support extracting to a writable stream that supports seeking
    /// <paramref name="outputStream"/> must be already opened and caller is responsible for closing it.
    /// </summary>
    /// <example>
    ///     <code>
    ///         var asyncReader = GzipReader.CreateFor('path_to_input_file').AndExtractToStreamAsync(decompressedStream);
    ///     </code>
    /// </example>
    public UnzipAsyncToStream AndExtractToStreamAsync(Stream outputStream)
    {
        return new AsyncGZipFileToStream(new FileToStream(_inputFile, OutputStream.CreateOver(outputStream)));
    }
}
