using System.Diagnostics.CodeAnalysis;
using contoso.functional.patterns.result;

namespace contoso.utility.compression.entities;

/// <summary>Defines a wrapper that encapsulates output stream validation.</summary>
public interface IOutputStream
{
    /// <summary>The validated input stream or an empty stream if invalid.</summary>
    Stream Stream { get; }
    /// <summary>Invoke this before each stream access to ensure it has not been closed, even if initially validated.</summary>
    Result<IOutputStream> Verify();
    /// <summary>Invoke this at write completion to ensure the stream has not been closed and has content.</summary>
    Result<long> VerifyLength();
    /// <summary>Attempt to reset the current position of the output stream to zero.</summary>
    bool TryResetPositionToZero();
    /// <summary>Attempt to set trim the stream from the current position, and then reset the current position of the output stream to zero.</summary>
    bool TryTrimAndResetPositionToZero();
}

/// <summary>Wraps generic initial output stream validation and also allows operation to check output stream has not been closed or disposed before accessing it.</summary>
/// <remarks>
/// Usually this simply wraps an opened stream for the decompressed output which caller is responsible for opening and disposing.
/// In some cases, the internal api creates and manages a FileStream as the abstraction over an output file.
/// </remarks>
internal class OutputStream : IOutputStream
{
    Result<IOutputStream> _faulted = default;
    readonly Stream? _outputStream;

    OutputStream(Stream? outputStream, Result<IOutputStream> faulted = default) => (_outputStream, _faulted) = (outputStream, faulted);

    /// <summary>Factory method.</summary>
    public static OutputStream CreateOver(Stream? inputStream)
    {
        if (inputStream == null)
            return new OutputStream(null, Result<IOutputStream>.WithError(Error.OutputStreamIsNull));

        try
        {
            if (!inputStream.CanWrite)
                return new OutputStream(inputStream, Result<IOutputStream>.WithError(Error.OutputStreamDoesNotSupportWrite));
        }
        catch (Exception ex)
        {
            return new OutputStream(inputStream, Result<IOutputStream>.WithError(Error.ExceptionWasThrown, ex));
        }

        return new OutputStream(inputStream);
    }

    /// <summary>The validated output stream or an empty stream if invalid.</summary>
    public Stream Stream { [return: NotNull] get => _outputStream ?? CreateEmpty(); }

    /// <summary>Invoke this before each stream access to ensure it has not been closed, even if initially validated.</summary>
    public Result<IOutputStream> Verify()
    {
        if (_faulted.Initialized)
            return _faulted;

        if (_outputStream == null)
            return Result<IOutputStream>.WithError(Error.InputStreamIsNull);

        try
        {
            if (!_outputStream.CanWrite)
                return Result<IOutputStream>.WithError(Error.OutputStreamDoesNotSupportWrite);

            return Result<IOutputStream>.WithSuccess(this);
        }
        catch (ObjectDisposedException ex)
        {
            return Result<IOutputStream>.WithError(Error.OutputStreamIsDisposed, ex);
        }
        catch (Exception ex)
        {
            return Result<IOutputStream>.WithError(Error.ExceptionWasThrown, ex);
        }
    }

    /// <summary>Invoke this at write completion to ensure the stream has not been closed and has content.</summary>
    public Result<long> VerifyLength()
    {
        if (_outputStream == null)
            return Result<long>.WithError(Error.OutputStreamIsNull);

        try
        {
            long length = _outputStream.Length;
            return length > 0 ? Result<long>.WithSuccess(length) : Result<long>.WithError(Error.OutputStreamIsEmpty);
        }
        catch (Exception ex)
        {
            return Result<long>.WithError(Error.ExceptionWasThrown, ex);
        }
    }

    /// <summary>Attempt to reset the current position of the output stream to zero.</summary>
    /// <returns>True if stream can seek and no error is thrown, otherwise false.</returns>
    public bool TryResetPositionToZero()
    {
        try
        {
            if (_outputStream != null && _outputStream.CanSeek)
            {
                _outputStream.Seek(0, SeekOrigin.Begin);
                return true;
            }
        }
        catch { }
        return false;
    }

    /// <summary>Attempt to set trim the stream from the current position, and then reset the current position of the output stream to zero.</summary>
    /// <remarks>Useful after writing to a caller-managed opened stream for the second attempt, where the first attempt may have written garbage to the end of the stream.</remarks>
    /// <returns>True if stream can seek and no error is thrown, otherwise false.</returns>
    public bool TryTrimAndResetPositionToZero()
    {
        bool trimmed = false;
        bool reset = false;
        try
        {
            if (_outputStream != null && _outputStream.CanSeek && _outputStream.CanWrite)
            {
                try
                {
                    if (_outputStream.Position > 0 && _outputStream.Position < _outputStream.Length)
                        _outputStream.SetLength(_outputStream.Position);
                    trimmed = true;
                }
                catch { trimmed = false; }

                _outputStream.Seek(0, SeekOrigin.Begin);
                reset = true;
            }
        }
        catch { reset = false; }

        return trimmed && reset;
    }

    // returns true for CanRead/CanWrite/CanSeek with Length = 0
    static MemoryStream CreateEmpty() => new MemoryStream(Array.Empty<byte>());
}
