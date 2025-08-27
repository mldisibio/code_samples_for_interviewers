using contoso.utility.compression.deflate.decompressors;
using contoso.functional.patterns.result;
using contoso.utility.compression.entities;

namespace contoso.utility.compression.deflate;

/// <summary>Asynchronously decompresses a raw deflate stream with any compression header or footer removed.</summary>
internal sealed class DeflateAsyncReader
{
    /// <summary>Actual asynchronous decompression code for any two opened input and output streams.</summary>
    internal static async Task<Result<StreamToStream>> DecompressCoreAsync(StreamToStream io)
    {
        try
        {
            return await io.Verify()
                           .AsAsyncResult()
                           .OnSuccessUseAsync(factory: _ => new AsyncDeflateStreamReader(),
                                              worker: reader => reader.DecompressAsync(io))
                           .ConfigureAwait(false);
        }
        catch (Exception inflateEx)
        {
            return Result<StreamToStream>.WithError(Error.ExceptionWasThrown, inflateEx);
        }
    }
}
