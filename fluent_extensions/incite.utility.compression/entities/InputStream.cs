using System.Diagnostics.CodeAnalysis;
using contoso.functional.patterns.result;

namespace contoso.utility.compression.entities;

/// <summary>Defines a wrapper that encapsulates input stream validation.</summary>
public interface IInputStream
{
    /// <summary>The validated input stream or an empty stream if invalid.</summary>
    Stream Stream { get; }
    /// <summary>Invoke this before each stream access to ensure it has not been closed, even if initially validated.</summary>
    Result<IInputStream> Verify();
    /// <summary>Make a copy of the full, open input stream, usually so that the original stream is not closed by failed decompression attempts.</summary>
    DisposableResult<MemoryStream> CopyToMemory();
    /// <summary>Make a copy of the open input stream trimming <paramref name="trimHeaderCount"/> bytes from start and <paramref name="trimFooterCount"/> bytes from end.</summary>
    DisposableResult<MemoryStream> TrimToMemory(int trimHeaderCount, int trimFooterCount);
    /// <summary>Asynchronously copy the full, open input stream, usually so that the original stream is not closed by failed decompression attempts.</summary>
    Task<DisposableResult<MemoryStream>> CopyToMemoryAsync();
    /// <summary>Asynchronously copy the open input stream trimming <paramref name="trimHeaderCount"/> bytes from start and <paramref name="trimFooterCount"/> bytes from end.</summary>
    Task<DisposableResult<MemoryStream>> TrimToMemoryAsync(int trimHeaderCount, int trimFooterCount);
}

/// <summary>Wraps generic initial input stream validation and also allows operation to check input stream has not been closed or disposed before accessing it.</summary>
/// <remarks>
/// Usually this simply wraps an opened stream of compressed bytes which caller is responsible for opening and disposing.
/// In some cases, the internal api creates and manages a copy of the original input stream, for decompression retry purposes,
/// and also creates and manages a FileStream as the abstraction over an input file.
/// </remarks>
internal class InputStream : IInputStream
{
    Result<IInputStream> _faulted = default;
    readonly Stream? _inputStream;

    InputStream(Stream? inputStream, Result<IInputStream> faulted = default) => (_inputStream, _faulted) = (inputStream, faulted);

    /// <summary>Factory method.</summary>
    public static InputStream CreateOver(Stream? inputStream)
    {
        if (inputStream == null)
            return new InputStream(null, Result<IInputStream>.WithError(Error.InputStreamIsNull));

        try
        {
            if (!inputStream.CanRead)
                return new InputStream(inputStream, Result<IInputStream>.WithError(Error.InputStreamDoesNotSupportRead));
            if (!inputStream.CanSeek)
                return new InputStream(inputStream, Result<IInputStream>.WithError(Error.InputStreamDoesNotSupportSeek));
            if (inputStream.Length == 0)
                return new InputStream(inputStream, Result<IInputStream>.WithError(Error.InputStreamIsEmpty));
        }
        catch (Exception ex)
        {
            return new InputStream(inputStream, Result<IInputStream>.WithError(Error.ExceptionWasThrown, ex));
        }

        return new InputStream(inputStream);
    }

    /// <summary>The validated input stream or an empty stream if invalid.</summary>
    public Stream Stream { [return: NotNull] get => _inputStream ?? CreateEmpty(); }

    /// <summary>Invoke this before each stream access to ensure it has not been closed, even if initially validated.</summary>
    public Result<IInputStream> Verify()
    {
        if (_faulted.Initialized)
            return _faulted;

        if (_inputStream == null)
            return Result<IInputStream>.WithError(Error.InputStreamIsNull);

        try
        {
            if (!_inputStream.CanRead)
                return Result<IInputStream>.WithError(Error.InputStreamDoesNotSupportRead);
            if (!_inputStream.CanSeek)
                return Result<IInputStream>.WithError(Error.InputStreamDoesNotSupportSeek);
            if (_inputStream.Length == 0)
                return Result<IInputStream>.WithError(Error.InputStreamIsEmpty);

            return Result<IInputStream>.WithSuccess(this);
        }
        catch (ObjectDisposedException ex)
        {
            return Result<IInputStream>.WithError(Error.InputStreamIsDisposed, ex);
        }
        catch (Exception ex)
        {
            return Result<IInputStream>.WithError(Error.ExceptionWasThrown, ex);
        }
    }

    /// <summary>Make a copy of the full, open input stream, usually so that the original stream is not closed by failed decompression attempts.</summary>
    public DisposableResult<MemoryStream> CopyToMemory() => TrimToMemory(0, 0);

    /// <summary>Make a copy of the open input stream trimming <paramref name="trimHeaderCount"/> bytes from start and <paramref name="trimFooterCount"/> bytes from end.</summary>
    public DisposableResult<MemoryStream> TrimToMemory(int trimHeaderCount, int trimFooterCount)
        => Verify().MapResultValueTo((input) => input.Stream)
                   .OnSuccess((input) => Copy(input, trimHeaderCount, trimFooterCount))
                   .AsDisposableResult();

    static Result<MemoryStream> Copy(Stream input, int trimHeaderCount, int trimFooterCount)
    {
        long bytesToCopy = input.Length - trimHeaderCount - trimFooterCount;

        if(bytesToCopy > int.MaxValue)
            return Result<MemoryStream>.WithError(Error.InputStreamLengthNotSupportedByMemoryStream);
        if(bytesToCopy <= 0)
            return Result<MemoryStream>.WithError(Error.InputStreamTrimmedLessThanLength);

        try
        {
            input.Seek(trimHeaderCount, SeekOrigin.Begin);
            (bool Success, MemoryStream MemoryStream) result = input.TryCopyAsMemoryStream((int)bytesToCopy);
            if (result.Success)
                return Result<MemoryStream>.WithSuccess(result.MemoryStream);
            else
            {
                result.MemoryStream?.Dispose();
                return Result<MemoryStream>.WithError(Error.ReturnedFalse);
            }
        }
        catch (Exception copyEx)
        {
            return Result<MemoryStream>.WithError(Error.ExceptionWasThrown, copyEx);
        }
        finally
        {
            input.Seek(0, SeekOrigin.Begin);
        }
    }

    /// <summary>Asynchronously copy the full, open input stream, usually so that the original stream is not closed by failed decompression attempts.</summary>
    public Task<DisposableResult<MemoryStream>> CopyToMemoryAsync() => TrimToMemoryAsync(0, 0);

    /// <summary>Asynchronously copy the open input stream trimming <paramref name="trimHeaderCount"/> bytes from start and <paramref name="trimFooterCount"/> bytes from end.</summary>
    public async Task<DisposableResult<MemoryStream>> TrimToMemoryAsync(int trimHeaderCount, int trimFooterCount)
    {
        var memResult = await Verify().MapResultValueTo((input) => input.Stream)
                                      .OnSuccessAsync((input) => CopyAsync(input, trimHeaderCount, trimFooterCount))
                                      .ConfigureAwait(false);
        return memResult.AsDisposableResult();
    }

    static async Task<Result<MemoryStream>> CopyAsync(Stream input, int trimHeaderCount, int trimFooterCount)
    {
        long bytesToCopy = input.Length - trimHeaderCount - trimFooterCount;

        if (bytesToCopy > int.MaxValue)
            return Result<MemoryStream>.WithError(Error.InputStreamLengthNotSupportedByMemoryStream);
        if (bytesToCopy <= 0)
            return Result<MemoryStream>.WithError(Error.InputStreamTrimmedLessThanLength);

        try
        {
            input.Seek(trimHeaderCount, SeekOrigin.Begin);
            (bool Success, MemoryStream MemoryStream) result = await input.TryCopyAsMemoryStreamAsync((int)bytesToCopy).ConfigureAwait(false);
            if (result.Success)
                return Result<MemoryStream>.WithSuccess(result.MemoryStream);
            else
            {
                result.MemoryStream?.Dispose();
                return Result<MemoryStream>.WithError(Error.ReturnedFalse);
            }
        }
        catch (Exception copyEx)
        {
            return Result<MemoryStream>.WithError(Error.ExceptionWasThrown, copyEx);
        }
    }

    // returns true for CanRead/CanWrite/CanSeek with Length = 0
    static MemoryStream CreateEmpty() => new MemoryStream(Array.Empty<byte>());
}


