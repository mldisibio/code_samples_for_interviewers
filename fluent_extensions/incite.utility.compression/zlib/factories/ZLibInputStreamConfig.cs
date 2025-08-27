using contoso.utility.compression.entities;
using contoso.utility.compression.zlib.decompressors;

namespace contoso.utility.compression.zlib;

/// <summary>Fluent configuration wrapper.</summary>
internal class ZLibInputStreamConfig : IZLibStreamConfig
{
    readonly IInputStream _inputStream;

    /// <summary>Initialized with a zlib input stream from <see cref="ZLibReader"/> factory method.</summary>
    internal ZLibInputStreamConfig(in IInputStream inputStream) => _inputStream = inputStream;

    /// <summary>Extract the zlib stream to the full path given by <paramref name="outputFileName"/>.</summary>
    /// <example>
    ///     <code>
    ///         var reader = ZLibReader.CreateFor(inputStream).AndExtractToFile('path_to_output_file');
    ///     </code>
    /// </example>
    public ZInflateToFile AndExtractToFile(string outputFileName)
    {
        IOutputFile outputPath = OutputFile.CreateOver(outputFileName);
        return new ZLibStreamToFile(new StreamToFile(_inputStream, outputPath));
    }

    /// <summary>Asynchronously extract the zlib stream to the full path given by <paramref name="outputFileName"/>.</summary>
    /// <example>
    ///     <code>
    ///         var asyncReader = ZLibReader.CreateFor(inputStream).AndExtractToFileAsync('path_to_output_file');
    ///     </code>
    /// </example>
    public ZInflateAsyncToFile AndExtractToFileAsync(string outputFileName)
    {
        IOutputFile outputPath = OutputFile.CreateOver(outputFileName);
        return new AsyncZLibStreamToFile(new StreamToFile(_inputStream, outputPath));
    }

    /// <summary>
    /// Extract the zlib stream to the given <paramref name="outputStream"/>.
    /// This is intended to support extracting to a writable stream that supports seeking
    /// <paramref name="outputStream"/> must be already opened and caller is responsible for closing it.
    /// </summary>
    /// <example>
    ///     <code>
    ///         var reader = ZLibReader.CreateFor(inputStream).AndExtractToFileAsync(outputStream);
    ///     </code>
    /// </example>
    public ZInflateToStream AndExtractToStream(Stream outputStream)
    {
        return new ZLibStreamToStream(new StreamToStream(_inputStream, OutputStream.CreateOver(outputStream)));
    }

    /// <summary>
    /// Asynchronously extract the zlib stream to the given <paramref name="outputStream"/>.
    /// This is intended to support extracting to a writable stream that supports seeking
    /// <paramref name="outputStream"/> must be already opened and caller is responsible for closing it.
    /// </summary>
    /// <example>
    ///     <code>
    ///         var asyncReader = ZLibReader.CreateFor(inputStream).AndExtractToFileAsync(outputStream);
    ///     </code>
    /// </example>
    public ZInflateAsyncToStream AndExtractToStreamAsync(Stream outputStream)
    {
        return new AsyncZLibStreamToStream(new StreamToStream(_inputStream, OutputStream.CreateOver(outputStream)));
    }

}
