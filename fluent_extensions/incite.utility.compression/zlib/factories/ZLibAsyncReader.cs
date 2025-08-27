using System.IO.Compression;
using contoso.functional.patterns.result;
using contoso.utility.compression.entities;

namespace contoso.utility.compression.zlib;
/// <summary>Asynchronously decompresses a zlib stream representing a single file (not an archive).</summary>
public sealed class ZLibAsyncReader
{
    /// <summary>
    /// Convenience method for asynchronous pre-operation check that the first two signature bytes of the file are [0x78 0x9C] or acceptable alternative
    /// and the length is long enough to accomodate the full zlib header and footer.
    /// </summary>
    public static async Task<bool> AppearsToBeZLib(string? filePath)
    {
        Result<ZLibFormatData> formatDataResult =
            await InputFile.CreateOver(filePath)
                           .Verify()
                           .OnSuccessAsync(TryReadHeaderAndFooterAsync)
                           .ConfigureAwait(false);

        return formatDataResult.Success && formatDataResult.Value.HasZLibSignature;
    }

    /// <summary>
    /// Convenience method for asynchronous pre-operation check that the first two signature bytes of the file are [0x78 0x9C] or acceptable alternative
    /// and the length is long enough to accomodate the full zlib header and footer.
    /// This is intended for accepting an in-memory stream that supports seeking and as such will not support another <see cref="ZLibStream"/> itself.
    /// </summary>
    public static async Task<bool> AppearsToBeZLib(Stream? zlibStream)
    {
        Result<ZLibFormatData> formatDataResult =
            await InputStream.CreateOver(zlibStream)
                             .Verify()
                             .MapResultValueTo((input) => input.Stream)
                             .OnSuccessAsync(TryReadHeaderAndFooterAsync)
                             .ConfigureAwait(false);

        return formatDataResult.Success && formatDataResult.Value.HasZLibSignature;
    }

    /// <summary>Attempt to asynchronously read the zlib header and footer from the given input file.</summary>
    internal static async Task<Result<ZLibFormatData>> TryReadHeaderAndFooterAsync(IInputFile input)
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
            return Result<ZLibFormatData>.WithError(Error.ExceptionWasThrown, openEx);
        }
    }

    /// <summary>Attempt to asynchronously read the zlib header and footer from the given <paramref name="input"/>.</summary>
    internal static Task<Result<ZLibFormatData>> TryReadHeaderAndFooterAsync(IInputStream input)
        => TryReadHeaderAndFooterAsync(input.Stream);

    internal static async Task<Result<ZLibFormatData>> TryReadHeaderAndFooterAsync(Stream? zlibStream)
    {
        try
        {
            if (zlibStream == null || zlibStream.Length < ZLibReader.MinRequiredZLibLength)
                return Result<ZLibFormatData>.WithError(Error.InputStreamLengthLessThanMinimumZLib);

            // read header
            zlibStream.Seek(0, SeekOrigin.Begin);
            byte[] header = new byte[ZLibFormatData.ZLibHeaderLength];
            if (!await header.TryFillAsyncFrom(zlibStream).ConfigureAwait(false))
                return Result<ZLibFormatData>.WithError(Error.CannotReadHeaderLengthFromStream);

            // read footer
            zlibStream.Seek((-1 * ZLibFormatData.ZLibFooterLength), SeekOrigin.End);
            byte[] footer = new byte[ZLibFormatData.ZLibFooterLength];
            if (!await footer.TryFillAsyncFrom(zlibStream).ConfigureAwait(false))
                return Result<ZLibFormatData>.WithError(Error.CannotReadFooterLengthFromStream);

            // header/footer are read (does not mean they are valid signature)
            return Result<ZLibFormatData>.WithSuccess(new ZLibFormatData(header, footer));
        }
        catch (Exception readEx)
        {
            return Result<ZLibFormatData>.WithError(Error.ExceptionWasThrown, readEx);
        }
        finally
        {
            // since stream is held open by caller, return position to zero
            try { zlibStream!.Seek(0, SeekOrigin.Begin); }
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
                ZLibStream decompressionStream;
                await using ((decompressionStream = new ZLibStream(ioPair.Input.Stream, CompressionMode.Decompress)).ConfigureAwait(false))
                {
                    await decompressionStream.CopyToAsync(ioPair.Output.Stream).ConfigureAwait(false);
                    await ioPair.Output.Stream.FlushAsync().ConfigureAwait(false);
                    ioPair.Output.TryTrimAndResetPositionToZero();
                }
                return Result<StreamToStream>.WithSuccess(ioPair);
            }
            catch (Exception inflateEx)
            {
                return Result<StreamToStream>.WithError(Error.ExceptionWasThrown, inflateEx);
            }
        }
    }
}
