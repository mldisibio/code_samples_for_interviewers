using System.IO.Compression;
using contoso.utility.compression.deflate.entities;
using contoso.functional.patterns.result;
using contoso.utility.compression.entities;

namespace contoso.utility.compression.deflate.decompressors;

/// <summary>
/// A reader that takes into account the frequent corruption errors encountered in compressed log file uploaded to Oasis
/// by reading the raw deflate stream, no header and no footer, in chunks.
/// Reading a corrupted deflate stream in one pass will throw an error, but reading it in chunks allows the decompression stream
/// to return as much content as possible.
/// </summary>
/// <remarks>
/// A solid deflate stream would not require such custom reading. The goal of this approach is to salvage partially decompressed
/// content from corrupted files. It turns out that both sqlite files and tar archives seem to be exceeding resilient, so this approach has some justification.
/// </remarks>
internal sealed partial class DeflateStreamReader : IDisposable
{
    internal const int BlockSize = 0x100;         //     256 bytes per block
    internal const int InitialCapacity = 0x80000; // 524,288 blocks = capacity for 128M

    readonly List<InflatedSegment> _segments;
    int _nextIndex;
    bool _alreadyDisposed;

    public DeflateStreamReader()
    {
        _segments = new List<InflatedSegment>(InitialCapacity);
    }

    public long TotalBytesRead => _segments.Sum(segment => segment.BytesRead);

    void Add(int bytesRead, byte[] buffer)
    {
        _segments.Add(new InflatedSegment(_nextIndex++, bytesRead, buffer));
    }

    /// <summary>Attempt to decompress all or as many bytes as possible from the raw deflate stream.</summary>
    public Result<StreamToStream> Decompress(StreamToStream io)
    {
        return io.Verify()
                 .MapResultValueTo(io => io.Input)
                 .OnSuccess(TryInflateToEndOrFirstError)
                 .MapResultValueTo(_ => io.Output)
                 .OnSuccess(CopyToOutputStream)
                 .MapResultValueTo(_ => io);

        Result<IOutputStream> CopyToOutputStream(IOutputStream output)
        {
            if (!_segments.Any(seg => seg.BytesRead > 0))
                return Result<IOutputStream>.WithError(Error.DeflateStreamNothingWasRead);
            try
            {
                for (int i = 0; i < _segments.Count; i++)
                {
                    InflatedSegment segment = _segments[i];
                    output.Stream.Write(segment.Content, 0, segment.BytesRead);

                }
                output.Stream.Flush();
                output.TryTrimAndResetPositionToZero();
                _segments.Clear();
                return Result<IOutputStream>.WithSuccess(output);
            }
            catch (Exception copyEx)
            {
                return Result<IOutputStream>.WithError(Error.ExceptionWasThrown, copyEx);
            }
        }
    }

    Result<IInputStream> TryInflateToEndOrFirstError(IInputStream inputStream)
    {
        // -----------------
        // Attempt to decompress all or as many bytes as possible from the raw deflate stream in blocks, until an error, if any, is encountered
        // -----------------
        DisposableResult<MemoryStream> copyInputResult = inputStream.CopyToMemory();
        try
        {
            return copyInputResult.AsResult()
                                  .TeeOnSuccess(inputCopy => inputCopy.Seek(0, SeekOrigin.Begin))
                                  .OnSuccessUse(factory: inputCopy => new DeflateStream(inputCopy, CompressionMode.Decompress),
                                                worker: ReadInBlocksToEndOrFirstError)
                                  .MapResultValueTo(_ => inputStream);
        }
        catch (Exception readEx)
        {
            return Result<IInputStream>.WithError(Error.ExceptionWasThrown, readEx);
        }
        finally
        {
            // close the copy of the input stream we made for internal use of attempt #1
            try { copyInputResult.DisposableValue.Dispose(); }
            catch { }
        }
    }

    Result<FirstPassResult> ReadInBlocksToEndOrFirstError(DeflateStream deflateStream)
    {
        // -----------------
        // read the deflate stream in blocks, storing each block buffer in an indexed list
        // (i.e. many small buffers instead of one giant MemoryStream whose initial capacity we don't know...
        //  the ultimate amount of memory needed is the same, but hopefully not stored in LOH);
        // expected outcomes are:
        // - we read all the way through without error (happy path) and we are done;
        // - we read until some block of bytes evokes a decompression error;
        // if an error is encountered, we'll have what was successfully decompressed saved in memory;
        // a failure here is only when no bytes are read at all;
        // -----------------
        long totalBytesRead = 0;
        try
        {
            int bytesRead = 0;
            do
            {
                byte[] buffer = new byte[BlockSize];
                totalBytesRead += (bytesRead = deflateStream.ReadWithCountInto(buffer));
                if(bytesRead > 0)
                    Add(bytesRead, buffer);
            } while (bytesRead != 0);

            return totalBytesRead > 0 
                   ? Result<FirstPassResult>.WithSuccess(new(totalBytesRead, false))
                   : Result<FirstPassResult>.WithError(Error.DeflateStreamNothingWasRead);
        }
        catch (Exception ex)
        {
            return totalBytesRead > 0
                   ? Result<FirstPassResult>.WithSuccess(new(totalBytesRead, true))
                   : Result<FirstPassResult>.WithError(Error.DeflateStreamNothingWasRead, ex);
        }
    }

    void Dispose(bool managed)
    {
        if (!_alreadyDisposed)
        {
            _alreadyDisposed = true;
            if (managed)
            {
                // TODO: dispose managed state (managed objects)
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _segments.Clear();
        }
    }

    /// <summary>Cleanup memory resources.</summary>
    public void Dispose()
    {
        Dispose(managed: true);
        GC.SuppressFinalize(this);
    }
}
