
namespace contoso.utility.compression;

/// <summary>Helpe extensions for reading a stream into a buffer.</summary>
public static class StreamExtensions
{
    const int _endOfStream = 0;

    /// <summary>
    /// Safe buffer read for non async stream operation
    /// successful when all elements of <paramref name="buffer"/> are filled by reading from <paramref name="src"/>
    /// It is expected that caller will open and close the <paramref name="src"/> stream
    /// AND 'seek' to the desired starting position.
    /// </summary>
    /// <returns>True if all elements of <paramref name="buffer"/> are filled by reading from <paramref name="src"/>.</returns>
    public static bool TryFillFrom(this byte[] buffer, Stream src)
    {
        if (src == null || buffer == null || !src.CanRead)
            return false;

        return buffer.Length == src.ReadWithCountInto(buffer, buffer.Length);
    }

    /// <summary>
    /// Safe buffer read for async stream operation
    /// successful when all elements of <paramref name="buffer"/> are filled by reading from <paramref name="src"/>
    /// It is expected that caller will open and close the <paramref name="src"/> stream
    /// AND 'seek' to the desired starting position.
    /// </summary>
    /// <returns>True if all elements of <paramref name="buffer"/> are filled by reading from <paramref name="src"/>.</returns>
    public static Task<bool> TryFillAsyncFrom(this byte[] buffer, Stream src) => buffer == null ? Task.FromResult(false) : TryFillAsyncFrom(buffer.AsMemory(), src);

    /// <summary>
    /// Safe buffer read for async stream operation
    /// successful when all elements of <paramref name="buffer"/> are filled by reading from <paramref name="src"/>
    /// It is expected that caller will open and close the <paramref name="src"/> stream
    /// AND 'seek' to the desired starting position.
    /// </summary>
    /// <returns>True if all elements of <paramref name="buffer"/> are filled by reading from <paramref name="src"/>.</returns>
    public static async Task<bool> TryFillAsyncFrom(this Memory<byte> buffer, Stream src)
    {
        if (src == null || !src.CanRead)
            return false;
        if (buffer.Length == 0)
            return true;

        return buffer.Length == await src.ReadAsync(buffer).ConfigureAwait(false);
    }

    /// <summary>
    /// Safe buffer read for non async stream operation to read <paramref name="buffer"/> length bytes from <paramref name="src"/>
    /// if the end of stream is not encountered first. Returns the total number of bytes read.
    /// It is expected that caller will open and close the <paramref name="src"/> stream
    /// AND 'seek' to the desired starting position.
    /// </summary>
    /// <returns>
    /// Number of bytes read into <paramref name="buffer"/> from <paramref name="src"/>, which can be less than <paramref name="buffer"/> length.
    /// </returns>
    public static int ReadWithCountInto(this Stream src, byte[] buffer) => ReadWithCountInto(src, buffer, (buffer?.Length).GetValueOrDefault());


    /// <summary>
    /// Safe buffer read for non async stream operation to read the smaller of <paramref name="maxCountToRead"/> or <paramref name="buffer"/> length bytes
    /// from <paramref name="src"/> if the end of stream is not encountered first. Returns the actual number of bytes read.
    /// It is expected that caller will open and close the <paramref name="src"/> stream
    /// AND 'seek' to the desired starting position.
    /// </summary>
    /// <returns>
    /// Number of bytes read into <paramref name="buffer"/> from <paramref name="src"/>, which can be less than <paramref name="buffer"/> length.
    /// </returns>
    public static int ReadWithCountInto(this Stream src, byte[] buffer, int maxCountToRead)
    {
        if (buffer == null || buffer.Length == 0 || src == null || !src.CanRead || maxCountToRead <= 0)
            return 0;

        // recommended pattern, accounting that 'Read' is free to return less bytes than requested
        int rc;
        int totalBytesRead = 0;
        int bytesRemaining = Math.Min(buffer.Length, maxCountToRead);
        do
        {
            totalBytesRead += (rc = src.Read(buffer, totalBytesRead, bytesRemaining));
            bytesRemaining -= rc;

        } while (bytesRemaining > 0 && rc != _endOfStream);

        return totalBytesRead;
    }

    /// <summary>
    /// Safe buffer read for async stream operation to read <paramref name="buffer"/> length bytes from <paramref name="src"/>
    /// if the end of stream is not encountered first. Returns the total number of bytes read.
    /// It is expected that caller will open and close the <paramref name="src"/> stream
    /// AND 'seek' to the desired starting position.
    /// </summary>
    /// <returns>
    /// Number of bytes read into <paramref name="buffer"/> from <paramref name="src"/>, which can be less than <paramref name="buffer"/> length.
    /// </returns>
    public static Task<int> ReadAsyncWithCountInto(this Stream src, byte[] buffer) => buffer == null ? Task.FromResult(0) : src.ReadAsyncWithCountInto(buffer, buffer.Length);

    /// <summary>
    /// Safe buffer read for async stream operation to read the smaller of <paramref name="maxCountToRead"/> or <paramref name="buffer"/> length bytes
    /// from <paramref name="src"/> if the end of stream is not encountered first. Returns the actual number of bytes read.
    /// It is expected that caller will open and close the <paramref name="src"/> stream
    /// AND 'seek' to the desired starting position.
    /// </summary>
    /// <returns>
    /// Number of bytes read into <paramref name="buffer"/> from <paramref name="src"/>, which can be less than <paramref name="buffer"/> length.
    /// </returns>
    public static async Task<int> ReadAsyncWithCountInto(this Stream src, byte[] buffer, int maxCountToRead)
    {
        if (buffer.Length == 0 || src == null || !src.CanRead || maxCountToRead <= 0)
            return 0;

        if (maxCountToRead >= buffer.Length)
            return await src.ReadAsync(buffer).ConfigureAwait(false);
        else
            return await src.ReadAsync(buffer, 0, maxCountToRead).ConfigureAwait(false);
    }

    /// <summary>
    /// Safe memory copy for non async stream operation.
    /// It is expected that caller will open and close the <paramref name="src"/> stream
    /// AND 'seek' to the desired starting position.
    /// </summary>
    /// <param name="src">The source stream to copy from, opened, and position set to where copy should start from.</param>
    /// <param name="bytesToRead">Number of bytes to read from the current position of <paramref name="src"/>, which may be less than the available bytes if wanting to trim from the end.</param>
    public static (bool Success, MemoryStream MemoryStream) TryCopyAsMemoryStream(this Stream src, int bytesToRead)
    {
        if (src == null || bytesToRead <= 0)
            return new(false, new MemoryStream(0));
        if (!src.CanRead)
            return new(false, new MemoryStream(0));

        byte[] buffer = new byte[bytesToRead];
        if (buffer.TryFillFrom(src))
        {
            MemoryStream? copy = null;
            try
            {
                // Write (instead of CopyTo) allows us to refactor with ArrayPool for byte[] and RecyclableMemory for memory stream, later
                copy = new MemoryStream(buffer.Length);
                copy.Write(buffer);
                copy.Seek(0, SeekOrigin.Begin);
                return new(true, copy);
            }
            catch
            {
                try { copy?.Dispose(); } catch { }
            }
        }
        return new(false, new MemoryStream(0));
    }

    /// <summary>
    /// Safe memory copy for non async stream operation.
    /// It is expected that caller will open and close the <paramref name="src"/> stream
    /// AND 'seek' to the desired starting position.
    /// </summary>
    /// <param name="src">The source stream to copy from, opened, and position set to where copy should start from.</param>
    /// <param name="bytesToRead">Number of bytes to read from the current position of <paramref name="src"/>, which may be less than the available bytes if wanting to trim from the end.</param>
    public static async Task<(bool Sucess, MemoryStream MemoryStream)> TryCopyAsMemoryStreamAsync(this Stream src, int bytesToRead)
    {
        if (src == null || bytesToRead <= 0)
            return new(false, new MemoryStream(0));
        if (!src.CanRead)
            return new(false, new MemoryStream(0));

        byte[] buffer = new byte[bytesToRead];
        if (await buffer.AsMemory().TryFillAsyncFrom(src).ConfigureAwait(false))
        {
            MemoryStream? copy = null;
            try
            {
                // WriteAsync (instead of CopyToAsync) allows us to refactor with ArrayPool for byte[] and RecyclableMemory for memory stream, later
                copy = new MemoryStream(buffer.Length);
                await copy.WriteAsync(buffer).ConfigureAwait(false);
                copy.Seek(0, SeekOrigin.Begin);
                return new(true, copy);
            }
            catch
            {
                try { copy?.Dispose(); } catch { }
            }
        }
        return new(false, new MemoryStream(0));
    }
}
