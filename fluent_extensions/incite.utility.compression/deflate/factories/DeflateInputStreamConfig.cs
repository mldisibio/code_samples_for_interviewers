using contoso.utility.compression.deflate.decompressors;
using contoso.utility.compression.entities;

namespace contoso.utility.compression.deflate;

/// <summary>Fluent configuration wrapper.</summary>
internal class DeflateInputStreamConfig : IDeflateStreamConfig
{
    readonly IInputStream _inputStream;

    /// <summary>Initialized with a deflated input stream from <see cref="DeflateReader"/> factory method.</summary>
    internal DeflateInputStreamConfig(in IInputStream inputStream) => _inputStream = inputStream;

    /// <summary>Extract the raw deflate stream to the full path given by <paramref name="outputFileName"/>.</summary>
    /// <example>
    ///     <code>
    ///         var deflateReader = DeflateReader.CreateFor(deflateStream).AndExtractToFile('path_to_output_file');
    ///     </code>
    /// </example>
    public InflateToFile AndExtractToFile(string outputFileName)
    {
        IOutputFile outputPath = OutputFile.CreateOver(outputFileName);
        return new DeflateStreamToFile(new StreamToFile(_inputStream, outputPath));
    }

    /// <summary>Asynchronously extract the raw deflate stream to the full path given by <paramref name="outputFileName"/>.</summary>
    /// <example>
    ///     <code>
    ///         var deflateReader = DeflateReader.CreateFor(deflateStream).AndExtractToFileAsync('path_to_output_file');
    ///     </code>
    /// </example>
    public InflateAsyncToFile AndExtractToFileAsync(string outputFileName)
    {
        IOutputFile outputPath = OutputFile.CreateOver(outputFileName);
        return new AsyncDeflateStreamToFile(new StreamToFile(_inputStream, outputPath));
    }

    /// <summary>
    /// Extract the raw deflate stream to the given <paramref name="outputStream"/>.
    /// This is intended to support extracting to a writable stream that supports seeking
    /// <paramref name="outputStream"/> must be already opened and caller is responsible for closing it.
    /// </summary>
    /// <example>
    ///     <code>
    ///         var deflateReader = DeflateReader.CreateFor(deflateStream).AndExtractToFileAsync(decompressedStream);
    ///     </code>
    /// </example>
    public InflateToStream AndExtractToStream(Stream outputStream)
    {
        return new DeflateStreamToStream(new StreamToStream(_inputStream, OutputStream.CreateOver(outputStream)));
    }

    /// <summary>
    /// Asynchronously extract the raw deflate stream to the given <paramref name="outputStream"/>.
    /// This is intended to support extracting to a writable stream that supports seeking
    /// <paramref name="outputStream"/> must be already opened and caller is responsible for closing it.
    /// </summary>
    /// <example>
    ///     <code>
    ///         var asyncReader = DeflateReader.CreateFor(deflateStream).AndExtractToFileAsync(decompressedStream);
    ///     </code>
    /// </example>
    public InflateAsyncToStream AndExtractToStreamAsync(Stream outputStream)
    {
        return new AsyncDeflateStreamToStream(new StreamToStream(_inputStream, OutputStream.CreateOver(outputStream)));
    }

}
