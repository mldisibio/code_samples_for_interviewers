using contoso.functional.patterns.result;
using contoso.utility.compression.entities;

namespace contoso.utility.compression.tar.entities;

internal readonly struct TarReaderInfo
{
    /// <summary>Private copy constructor.</summary>
    TarReaderInfo(StreamToDirectory ioConfig,
                  ExtractToDirectoryResult opResult,
                  TarHeaderBlock header,
                  long consumedCount,
                  IOutputFile? outputFile)
        => (OpResult, IO, HeaderBlock, ConsumedCount, ExtractedFile) = (opResult, ioConfig, header, consumedCount, outputFile!);

    /// <summary>Intialize as start of extraction with input and output meatadata, and the cummulative result log.</summary>
    public TarReaderInfo(StreamToDirectory ioConfig, ExtractToDirectoryResult opResult) : this(ioConfig, opResult, default, default, default) { }

    /// <summary>Copy to a new instance with the current header block from the input stream.</summary>
    public TarReaderInfo With(TarHeaderBlock header) => new(IO, OpResult, header, ConsumedCount, ExtractedFile);

    /// <summary>Copy to a new instance with the bytes consumed while extracting the file from the end of the header.</summary>
    public TarReaderInfo With(long bytesConsumed) => new(IO, OpResult, HeaderBlock, bytesConsumed, ExtractedFile);

    /// <summary>Copy to a new instance with the bytes consumed while extracting the file from the end of the header.</summary>
    public TarReaderInfo With(IOutputFile outputFile) => new(IO, OpResult, HeaderBlock, ConsumedCount, outputFile);

    /// <summary>The original input stream and output directory metadata.</summary>
    public StreamToDirectory IO { get; init; }
    /// <summary>The cummulative operation result and log.</summary>
    public ExtractToDirectoryResult OpResult { get; init; }
    /// <summary>The current header block in context.</summary>
    public TarHeaderBlock HeaderBlock { get; init; }
    /// <summary>The output file to be extracted from between the current header and the next.</summary>
    public IOutputFile ExtractedFile { get; init; }
    /// <summary>Bytes consumed while extracting the file from the end of the current header in context.</summary>
    public long ConsumedCount { get; init; }

    /// <summary>The underlying input stream (tar archive as a file stream).</summary>
    public Stream InputStream => IO.Input.Stream;
    /// <summary>Length remaining until end of stream.</summary>
    public long InputLengthAvailable => IO.Input.Stream.Length - IO.Input.Stream.Position;
    /// <summary>The wrapper around the output directory metadata.</summary>
    public IOutputDirectory OutputDirectory => IO.Output;
    /// <summary>Length (in bytes) listed in the current header of the file to be extracted.</summary>
    public long ExpectedFileSize => HeaderBlock.Size.GetValueOrDefault();

    /// <summary>Verify the input stream and output directory.</summary>
    public Result<TarReaderInfo> Verify()
    {
        var ioResult = IO.Verify();
        return ioResult.Success
               ? Result<TarReaderInfo>.WithSuccess(this)
               : Result<TarReaderInfo>.WithErrorFrom(ioResult);
    }
}

