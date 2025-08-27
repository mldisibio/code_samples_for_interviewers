
using contoso.utility.compression.entities;
using contoso.utility.compression.gzip.decompressors;

namespace contoso.utility.compression.gzip;

/// <summary>Fluent configuration wrapper.</summary>
internal class GZipInputStreamConfig : IGZipStreamConfig
{
    readonly IInputStream _inputStream;

    /// <summary>Initialized with a zipped input stream from <see cref="GZipReader"/> factory method.</summary>
    internal GZipInputStreamConfig(in IInputStream inputStream) => _inputStream = inputStream;

    /// <summary>Extract the zipped stream to the full path given by <paramref name="outputFileName"/>.</summary>
    /// <example>
    ///     <code>
    ///         var zipReader = GzipReader.CreateFor(zippedStream).AndExtractToFile('path_to_output_file');
    ///     </code>
    /// </example>
    public UnzipToFile AndExtractToFile(string outputFileName)
    {
        IOutputFile outputPath = OutputFile.CreateOver(outputFileName);
        return new GZipStreamToFile(new StreamToFile(_inputStream, outputPath));
    }

    /// <summary>Asynchronously extract the zipped stream to the full path given by <paramref name="outputFileName"/>.</summary>
    /// <example>
    ///     <code>
    ///         var asyncReader = GzipReader.CreateFor(zippedStream).AndExtractToFileAsync('path_to_output_file');
    ///     </code>
    /// </example>
    public UnzipAsyncToFile AndExtractToFileAsync(string outputFileName)
    {
        IOutputFile outputPath = OutputFile.CreateOver(outputFileName);
        return new AsyncGZipStreamToFile(new StreamToFile(_inputStream, outputPath));
    }

    /// <summary>
    /// Extract the zipped stream to the given <paramref name="outputStream"/>.
    /// This is intended to support extracting to a writable stream that supports seeking
    /// <paramref name="outputStream"/> must be already opened and caller is responsible for closing it.
    /// </summary>
    /// <example>
    ///     <code>
    ///         var zipReader = GzipReader.CreateFor(zippedStream).AndExtractToFileAsync(decompressedStream);
    ///     </code>
    /// </example>
    public UnzipToStream AndExtractToStream(Stream outputStream)
    {
        return new GZipStreamToStream(new StreamToStream(_inputStream, OutputStream.CreateOver(outputStream)));
    }

    /// <summary>
    /// Asynchronously extract the zipped stream to the given <paramref name="outputStream"/>.
    /// This is intended to support extracting to a writable stream that supports seeking
    /// <paramref name="outputStream"/> must be already opened and caller is responsible for closing it.
    /// </summary>
    /// <example>
    ///     <code>
    ///         var asyncReader = GzipReader.CreateFor(zippedStream).AndExtractToFileAsync(decompressedStream);
    ///     </code>
    /// </example>
    public UnzipAsyncToStream AndExtractToStreamAsync(Stream outputStream)
    {
        return new AsyncGZipStreamToStream(new StreamToStream(_inputStream, OutputStream.CreateOver(outputStream)));
    }

}
