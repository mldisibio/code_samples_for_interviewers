using System.IO.Compression;
using contoso.functional.patterns.result;
using contoso.utility.compression.entities;

namespace contoso.utility.compression.gzip;

/// <summary>Decompresses a gzip stream or file that holds only a single file (not an archive).</summary>
public sealed class GZipReader
{
    /// <summary>Minimum acceptable length for a gzip file assuming it has a header and a footer.</summary>
    public const int MinRequiredGZipLength = GZipFormatData.GZipHeaderLength + GZipFormatData.GZipFooterLength;
    internal const string NoGzipSignatureError = "No valid gzip signature detected";
    internal const string DecompressStartMsg = "START GZip Decompress";
    internal const string DecompressStartMsgNoCRC = "START GZip Decompress No CRC";
    internal const string DecompressWithRetryError = "Decompress failed. Retrying without CRC";
    internal const string DeflateFailedError = "Failed to decompress raw deflate stream";

    /// <summary>Configure a reader factory for the given <paramref name="inputFileName"/>.</summary>
    /// <param name="inputFileName">Full path to file content compressed with the gzip algorithm.</param>
    /// <param name="withHeaderCheck">True to check the header and throw an exception if the expected gzip signature is not present. Default is false.</param>
    /// <example><code>var reader = GzipReader.CreateFor('inputFile').AndExtractToFile('outputFile');</code></example>
    public static IGZipFileConfig CreateFor(string inputFileName, bool withHeaderCheck = false)
    {
        IInputFile inputFile = InputFile.CreateOver(inputFileName);
        
        if (withHeaderCheck)
        {
            // since caller is expecting a possible exception, we'll throw on invalid path as well
            var inputValidation = inputFile.Verify();
            if (!inputValidation.Success)
                throw new InvalidOperationException(inputValidation.ErrorMessage);

            if(!AppearsToBeGZip(inputFile.FullPath))
                throw new InvalidOperationException($"[{inputFile.FullPath}] {NoGzipSignatureError}");
        }
        return new GZipInputFileConfig(inputFile);
    }

    /// <summary>
    /// Configure a reader factory for the given <paramref name="zipStream"/>.
    /// This is intended for accepting an in-memory stream that supports seeking and as such 
    /// will not support another <see cref="GZipStream"/> itself. 
    /// <paramref name="zipStream"/> must be already opened and caller is responsible for closing it.
    /// </summary>
    /// <param name="zipStream">An open stream containing content compressed with the gzip algorithm.</param>
    /// <param name="withHeaderCheck">True to check the header and throw an exception if the expected gzip signature is not present. Default is false.</param>
    /// <example><code>var reader = GzipReader.CreateFor(inputStream).AndExtractToStream(outputStream);</code></example>
    public static IGZipStreamConfig CreateFor(Stream zipStream, bool withHeaderCheck = false)
    {
        IInputStream inputStream = InputStream.CreateOver(zipStream);
        if (withHeaderCheck)
        {
            var inputValidation = inputStream.Verify();
            if (!inputValidation.Success)
                throw new InvalidOperationException(inputValidation.ErrorMessage);

            if(!AppearsToBeGZip(inputStream.Stream))
                throw new InvalidOperationException(NoGzipSignatureError);
        }
        return new GZipInputStreamConfig(inputStream);
    }

    /// <summary>
    /// Convenience method for pre-operation check that the first two signature bytes of the file are [0x1F 0x8B] 
    /// and the length is long enough to accomodate the full zip header and footer.
    /// </summary>
    public static bool AppearsToBeGZip(string? filePath)
    {
        Result<GZipFormatData> formatDataResult = InputFile.CreateOver(filePath)
                                                           .Verify()
                                                           .OnSuccess(TryReadHeaderAndFooter);

        return formatDataResult.Success && formatDataResult.Value.HasGZipSignature;
    }

    /// <summary>
    /// Convenience method for pre-operation check that the first two signature bytes of the stream are [0x1F 0x8B] 
    /// and the length is long enough to accomodate the full zip header and footer.
    /// <paramref name="zipStream"/> must be already opened and caller must close it.
    /// This is intended for accepting an in-memory stream that supports seeking and as such will not support another <see cref="GZipStream"/> itself.
    /// </summary>
    public static bool AppearsToBeGZip(Stream? zipStream)
    {
        Result<GZipFormatData> formatDataResult = InputStream.CreateOver(zipStream)
                                                             .Verify()
                                                             .MapResultValueTo((input) => input.Stream)
                                                             .OnSuccess(TryReadHeaderAndFooter);
        return formatDataResult.Success && formatDataResult.Value.HasGZipSignature;
    }

    /// <summary>Attempt to read the gzip header and footer from the given input file. This call will block.</summary>
    internal static Result<GZipFormatData> TryReadHeaderAndFooter(IInputFile input)
    {
        try
        {
            using FileStream inputStream = input.OpenRead();
            return TryReadHeaderAndFooter(inputStream);
        }
        catch (Exception openEx)
        {
            return Result<GZipFormatData>.WithError(Error.ExceptionWasThrown, openEx);
        }
    }

    /// <summary>Attempt to read the gzip header and footer from the given <paramref name="input"/>. This call will block.</summary>
    internal static Result<GZipFormatData> TryReadHeaderAndFooter(IInputStream input) => TryReadHeaderAndFooter(input.Stream);

    /// <summary>Attempt to read the gzip header and footer from the already validated <paramref name="zipStream"/>. This call will block.</summary>
    internal static Result<GZipFormatData> TryReadHeaderAndFooter(Stream? zipStream)
    {
        try
        {
            if (zipStream == null || zipStream.Length < MinRequiredGZipLength)
                return Result<GZipFormatData>.WithError(Error.InputStreamLengthLessThanMinimumGZip);

            // read header
            zipStream.Seek(0, SeekOrigin.Begin);
            byte[] header = new byte[GZipFormatData.GZipHeaderLength];
            if (!header.TryFillFrom(zipStream))
                return Result<GZipFormatData>.WithError(Error.CannotReadHeaderLengthFromStream);

            // read footer
            zipStream.Seek((-1 * GZipFormatData.GZipFooterLength), SeekOrigin.End);
            byte[] footer = new byte[GZipFormatData.GZipFooterLength];
            if (!footer.TryFillFrom(zipStream))
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

    /// <summary>Actual decompression code for any two opened input and output streams.</summary>
    internal static Result<StreamToStream> DecompressCore(StreamToStream io)
    {
        return io.Verify().OnSuccess(InvokeDecompression);

        // local function to decompress the input stream to the output stream that can be composed with the stream validation
        static Result<StreamToStream> InvokeDecompression(StreamToStream ioPair)
        {
            try
            {
                using GZipStream decompressionStream = new GZipStream(ioPair.Input.Stream, CompressionMode.Decompress);
                decompressionStream.CopyTo(ioPair.Output.Stream);
                ioPair.Output.Stream.Flush();
                ioPair.Output.TryTrimAndResetPositionToZero();

                return Result<StreamToStream>.WithSuccess(ioPair);
            }
            catch (Exception unzipEx)
            {
                return Result<StreamToStream>.WithError(Error.ExceptionWasThrown, unzipEx);
            }
        }
    }
}
