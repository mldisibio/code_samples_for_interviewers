using contoso.utility.compression.deflate.decompressors;
using contoso.functional.patterns.result;
using contoso.utility.compression.entities;

namespace contoso.utility.compression.deflate;

/// <summary>Decompresses a raw deflate stream with any compression header or footer removed.</summary>
internal sealed class DeflateReader
{
    internal const string DecompressStartMsg = "START Raw Deflate Decompress";

    /// <summary>
    /// Configure a reader factory for the given <paramref name="deflateStream"/>.
    /// This is intended for accepting an in-memory stream that supports seeking and as such 
    /// will not support another <see cref="System.IO.Compression.DeflateStream"/> itself. 
    /// <paramref name="deflateStream"/> must be already opened and caller is responsible for closing it.
    /// </summary>
    /// <param name="deflateStream">An open stream containing content compressed with the deflate algorithm.</param>
    /// <example><code>var reader = DeflateReader.CreateFor(inputStream).AndExtractToStream(outputStream);</code></example>
    public static IDeflateStreamConfig CreateFor(Stream deflateStream)
    {
        IInputStream inputStream = InputStream.CreateOver(deflateStream);
        return new DeflateInputStreamConfig(inputStream);
    }

    /// <summary>Actual decompression code for any two opened input and output streams.</summary>
    internal static Result<StreamToStream> DecompressCore(StreamToStream io)
    {
        try
        {
            return io.Verify()
                     .OnSuccessUse(factory: _ => new DeflateStreamReader(),
                                   worker: reader => reader.Decompress(io));
        }
        catch (Exception inflateEx)
        {
            return Result<StreamToStream>.WithError(Error.ExceptionWasThrown, inflateEx);
        }
    }
}
