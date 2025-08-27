using System.IO.Compression;
using contoso.functional.patterns.result;
using contoso.utility.compression.entities;

namespace contoso.utility.compression.zlib;

/// <summary>Decompresses a zlib/defalte stream or file.</summary>
public sealed class ZLibReader

{    /// <summary>Minimum acceptable length for a zlib file assuming it has a header and a footer.</summary>
    public const int MinRequiredZLibLength = ZLibFormatData.ZLibHeaderLength + ZLibFormatData.ZLibFooterLength;
    internal const string NoZLibSignatureError = "No valid zlib signature detected";
    internal const string DecompressStartMsg = "START ZLib Decompress";
    internal const string DecompressStartMsgNoCRC = "START ZLib Decompress No CRC";
    internal const string DecompressWithRetryError = "Decompress failed. Retrying without CRC";
    internal const string DeflateFailedError = "Failed to decompress raw deflate stream";

    /// <summary>Configure a reader factory for the given <paramref name="inputFileName"/>.</summary>
    /// <param name="inputFileName">Full path to file content compressed with the zlib algorithm.</param>
    /// <param name="withHeaderCheck">True to check the header and throw an exception if the expected zlib signature is not present. Default is false.</param>
    /// <example><code>var reader = ZLibReader.CreateFor('inputFile').AndExtractToFile('outputFile');</code></example>
    public static IZLibFileConfig CreateFor(string inputFileName, bool withHeaderCheck = false)
    {
        IInputFile inputFile = InputFile.CreateOver(inputFileName);

        if (withHeaderCheck)
        {
            // since caller is expecting a possible exception, we'll throw on invalid path as well
            var inputValidation = inputFile.Verify();
            if (!inputValidation.Success)
                throw new InvalidOperationException(inputValidation.ErrorMessage);

            if (!AppearsToBeZLib(inputFile.FullPath))
                throw new InvalidOperationException($"[{inputFile.FullPath}] {NoZLibSignatureError}");
        }
        return new ZLibInputFileConfig(inputFile);
    }

    /// <summary>
    /// Configure a reader factory for the given <paramref name="zlibStream"/>.
    /// This is intended for accepting an in-memory stream that supports seeking and as such 
    /// will not support another <see cref="ZLibStream"/> itself. 
    /// <paramref name="zlibStream"/> must be already opened and caller is responsible for closing it.
    /// </summary>
    /// <param name="zlibStream">An open stream containing content compressed with the zlib algorithm.</param>
    /// <param name="withHeaderCheck">True to check the header and throw an exception if the expected zlib signature is not present. Default is false.</param>
    /// <example><code>var reader = ZLibReader.CreateFor(inputStream).AndExtractToStream(outputStream);</code></example>
    public static IZLibStreamConfig CreateFor(Stream zlibStream, bool withHeaderCheck = false)
    {
        IInputStream inputStream = InputStream.CreateOver(zlibStream);
        if (withHeaderCheck)
        {
            var inputValidation = inputStream.Verify();
            if (!inputValidation.Success)
                throw new InvalidOperationException(inputValidation.ErrorMessage);

            if (!AppearsToBeZLib(inputStream.Stream))
                throw new InvalidOperationException(NoZLibSignatureError);
        }
        return new ZLibInputStreamConfig(inputStream);
    }

    /// <summary>
    /// Convenience method for pre-operation check that the first two signature bytes of the file are [0x78 0x9C] or acceptable alternative
    /// and the length is long enough to accomodate the full zlib header and footer.
    /// </summary>
    public static bool AppearsToBeZLib(string? filePath)
    {
        Result<ZLibFormatData> formatDataResult = InputFile.CreateOver(filePath)
                                                           .Verify()
                                                           .OnSuccess(TryReadHeaderAndFooter);

        return formatDataResult.Success && formatDataResult.Value.HasZLibSignature;
    }

    /// <summary>
    /// Convenience method for pre-operation check that the first two signature bytes of the stream are [0x78 0x9C] or acceptable alternative
    /// and the length is long enough to accomodate the full zlib header and footer.
    /// <paramref name="zlibStream"/> must be already opened and caller must close it.
    /// This is intended for accepting an in-memory stream that supports seeking and as such will not support another <see cref="ZLibStream"/> itself.
    /// </summary>
    public static bool AppearsToBeZLib(Stream? zlibStream)
    {
        Result<ZLibFormatData> formatDataResult = InputStream.CreateOver(zlibStream)
                                                             .Verify()
                                                             .MapResultValueTo((input) => input.Stream)
                                                             .OnSuccess(TryReadHeaderAndFooter);
        return formatDataResult.Success && formatDataResult.Value.HasZLibSignature;
    }

    /// <summary>Attempt to read the zlib header and footer from the given input file. This call will block.</summary>
    internal static Result<ZLibFormatData> TryReadHeaderAndFooter(IInputFile input)
    {
        try
        {
            using FileStream zlibStream = input.OpenRead();
            return TryReadHeaderAndFooter(zlibStream);
        }
        catch (Exception openEx)
        {
            return Result<ZLibFormatData>.WithError(Error.ExceptionWasThrown, openEx);
        }
    }

    /// <summary>Attempt to read the zlib header and footer from the given <paramref name="zlibStream"/>. This call will block.</summary>
    internal static Result<ZLibFormatData> TryReadHeaderAndFooter(IInputStream zlibStream) => TryReadHeaderAndFooter(zlibStream.Stream);

    /// <summary>Attempt to read the zlib header and footer from the already validated <paramref name="zlibStream"/>. This call will block.</summary>
    internal static Result<ZLibFormatData> TryReadHeaderAndFooter(Stream? zlibStream)
    {
        try
        {
            if (zlibStream == null || zlibStream.Length < MinRequiredZLibLength)
                return Result<ZLibFormatData>.WithError(Error.InputStreamLengthLessThanMinimumZLib);

            // read header
            zlibStream.Seek(0, SeekOrigin.Begin);
            byte[] header = new byte[ZLibFormatData.ZLibHeaderLength];
            if (!header.TryFillFrom(zlibStream))
                return Result<ZLibFormatData>.WithError(Error.CannotReadHeaderLengthFromStream);

            // read footer
            zlibStream.Seek((-1 * ZLibFormatData.ZLibFooterLength), SeekOrigin.End);
            byte[] footer = new byte[ZLibFormatData.ZLibFooterLength];
            if (!footer.TryFillFrom(zlibStream))
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

    /// <summary>Actual decompression code for any two opened input and output streams.</summary>
    internal static Result<StreamToStream> DecompressCore(StreamToStream io)
    {
        return io.Verify().OnSuccess(InvokeDecompression);

        // local function to decompress the input stream to the output stream that can be composed with the stream validation
        static Result<StreamToStream> InvokeDecompression(StreamToStream ioPair)
        {
            try
            {
                using ZLibStream decompressionStream = new ZLibStream(ioPair.Input.Stream, CompressionMode.Decompress);
                decompressionStream.CopyTo(ioPair.Output.Stream);
                ioPair.Output.Stream.Flush();
                ioPair.Output.TryTrimAndResetPositionToZero();

                return Result<StreamToStream>.WithSuccess(ioPair);
            }
            catch (Exception zlibEx)
            {
                return Result<StreamToStream>.WithError(Error.ExceptionWasThrown, zlibEx);
            }
        }
    }
}
