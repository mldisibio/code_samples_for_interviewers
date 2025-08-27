using System.IO.Compression;
using contoso.functional.patterns.result;
using contoso.utility.compression.entities;

namespace contoso.utility.compression.gzip;
/// <summary>Asynchronously decompresses a gzip stream representing a single file (not an archive).</summary>
public sealed class GZipAsyncReader
{
    /// <summary>
    /// Convenience method for asynchronous pre-operation check that the first two signature bytes of the file are [0x1F 0x8B] 
    /// and the length is long enough to accomodate the full zip header and footer.
    /// </summary>
    public static async Task<bool> AppearsToBeGZip(string? filePath)
    {
        Result<GZipFormatData> formatDataResult =
            await InputFile.CreateOver(filePath)
                           .Verify()
                           .OnSuccessAsync(TryReadHeaderAndFooterAsync)
                           .ConfigureAwait(false);

        return formatDataResult.Success && formatDataResult.Value.HasGZipSignature;
    }

    /// <summary>
    /// Convenience method for asynchronous pre-operation check that the first two signature bytes of the file are [0x1F 0x8B] 
    /// and the length is long enough to accomodate the full zip header and footer.
    /// This is intended for accepting an in-memory stream that supports seeking and as such will not support another <see cref="System.IO.Compression.GZipStream"/> itself.
    /// </summary>
    public static async Task<bool> AppearsToBeGZip(Stream? zipStream)
    {
        Result<GZipFormatData> formatDataResult = 
            await InputStream.CreateOver(zipStream)
                             .Verify()
                             .MapResultValueTo((input) => input.Stream)
                             .OnSuccessAsync(TryReadHeaderAndFooterAsync)
                             .ConfigureAwait(false);

        return formatDataResult.Success && formatDataResult.Value.HasGZipSignature;
    }

    /// <summary>Attempt to asynchronously read the gzip header and footer from the given input file.</summary>
    internal static async Task<Result<GZipFormatData>> TryReadHeaderAndFooterAsync(IInputFile input)
    {
        try
        {
            FileStream inputStream;
            await using ((inputStream = input.OpenReadForAsync()).ConfigureAwait(false))
            {
                return await TryReadHeaderAndFooterAsync(inputStream).ConfigureAwait(false);
            }
        }
        catch (Exception openEx)
        {
            return Result<GZipFormatData>.WithError(Error.ExceptionWasThrown, openEx);
        }
    }

    /// <summary>Attempt to asynchronously read the gzip header and footer from the given <paramref name="input"/>.</summary>
    internal static Task<Result<GZipFormatData>> TryReadHeaderAndFooterAsync(IInputStream input) 
        => TryReadHeaderAndFooterAsync(input.Stream);

    internal static async Task<Result<GZipFormatData>> TryReadHeaderAndFooterAsync(Stream? zipStream)
    {
        try
        {
            if (zipStream == null || zipStream.Length < GZipReader.MinRequiredGZipLength)
                return Result<GZipFormatData>.WithError(Error.InputStreamLengthLessThanMinimumGZip);

            // read header
            zipStream.Seek(0, SeekOrigin.Begin);
            byte[] header = new byte[GZipFormatData.GZipHeaderLength];
            if (!await header.TryFillAsyncFrom(zipStream).ConfigureAwait(false))
                return Result<GZipFormatData>.WithError(Error.CannotReadHeaderLengthFromStream);

            // read footer
            zipStream.Seek((-1 * GZipFormatData.GZipFooterLength), SeekOrigin.End);
            byte[] footer = new byte[GZipFormatData.GZipFooterLength];
            if (!await footer.TryFillAsyncFrom(zipStream).ConfigureAwait(false))
                return Result<GZipFormatData>.WithError(Error.CannotReadFooterLengthFromStream);

            // header/footer are read (does not mean they are valid signature)
            return Result<GZipFormatData>.WithSuccess(new GZipFormatData(header, footer));
        }
        catch (Exception readEx)
        {
            return Result<GZipFormatData>.WithError(Error.ExceptionWasThrown, readEx);
        }
        finally
        {
            // since stream is held open by caller, return position to zero
            try { zipStream!.Seek(0, SeekOrigin.Begin); }
            catch { }
        }
    }

    /// <summary>Actual asynchronous decompression code for any two opened input and output streams.</summary>
    internal static async Task<Result<StreamToStream>> DecompressCoreAsync(StreamToStream io)
    {
        return await io.Verify()
                       .OnSuccessAsync(InvokeDecompressionAsync)
                       .ConfigureAwait(false);

        // local function to decompress the input stream to the output stream that can be composed with the stream validation
        static async Task<Result<StreamToStream>> InvokeDecompressionAsync(StreamToStream ioPair)
        {
            try
            {
                GZipStream decompressionStream;
                await using ((decompressionStream = new GZipStream(ioPair.Input.Stream, CompressionMode.Decompress)).ConfigureAwait(false))
                {
                    await decompressionStream.CopyToAsync(ioPair.Output.Stream).ConfigureAwait(false);
                    await ioPair.Output.Stream.FlushAsync().ConfigureAwait(false);
                    ioPair.Output.TryTrimAndResetPositionToZero();
                }
                return Result<StreamToStream>.WithSuccess(ioPair);
            }
            catch (Exception unzipEx)
            {
                return Result<StreamToStream>.WithError(Error.ExceptionWasThrown, unzipEx);
            }
        }
    }
}
