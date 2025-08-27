using System.Buffers.Binary;

namespace contoso.extraction.tags.io;

internal static class ReaderUtil
{
    public const long EpochSeconds20150101 = 1420070400L;

    /// <summary>Convert the hex pairs from <paramref name="hexString"/> to their corresponding bytes, in the same order.</summary>
    public static bool AsHexToByteArray(this string hexString, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        try
        {
            if (hexString.IsNullOrEmptyString())
                return false;
            if (hexString.Length % 2 != 0)
                return false;
            bytes = hexString.TakeBy(2)
                             .Select(pair => new string(pair.ToArray()))
                             .Select(hexPair => Convert.ToByte(hexPair, 16))
                             .ToArray();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Convert <paramref name="length"/> bytes from <paramref name="buffer"/>, starting at index <paramref name="start"/>,
    /// to their corresponding hex representation, in the same order.
    /// </summary>
    public static string AsByteArrayToHex(this byte[] buffer, int start, int length)
    {
        if (buffer.IsNullOrEmpty())
            return string.Empty;

        return string.Join("", buffer.Skip(start).Take(length).Select(b => $"{b:X2}"));
    }

    /// <summary>Return the actual 64 bits as stored (LittleEndian) for the lexically BigEndian (left-to-right) eight bytes representated by <paramref name="buffer"/>.</summary>
    public static ulong AsLexicalBytesToStored64(this ReadOnlySpan<byte> buffer) => BinaryPrimitives.ReadUInt64BigEndian(buffer);

    /// <summary>Return the actual 32 bits as stored (LittleEndian) for the lexically BigEndian (left-to-right) four bytes representated by <paramref name="buffer"/>.</summary>
    public static uint AsLexicalBytesToStored32(this ReadOnlySpan<byte> buffer) => BinaryPrimitives.ReadUInt32BigEndian(buffer);

    /// <summary>Return the actual 16 bits as stored (LittleEndian) for the lexically BigEndian (left-to-right) two bytes representated by <paramref name="buffer"/>.</summary>
    public static ushort AsLexicalBytesToStored16(this ReadOnlySpan<byte> buffer) => BinaryPrimitives.ReadUInt16BigEndian(buffer);

    /// <summary>Convert the actual 64 bits in <paramref name="storedValue"/>> as stored (LittleEndian) to an eight byte representation read left-to-right (BigEndian).</summary>
    public static byte[] AsStored64ToLexicalBytes(this ulong storedValue) => BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(storedValue));

    /// <summary>Convert the actual 32 bits in <paramref name="storedValue"/>> as stored (LittleEndian) to a four byte representation read left-to-right (BigEndian).</summary>
    public static byte[] AsStored32ToLexicalBytes(this uint storedValue) => BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(storedValue));

    /// <summary>Convert the actual 16 bits in <paramref name="storedValue"/>> as stored (LittleEndian) to an two byte representation read left-to-right (BigEndian).</summary>
    public static byte[] AsStored16ToLexicalBytes(this ushort storedValue) => BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(storedValue));

    /// <summary>Convert the binary representation of the tag identifier value into it's recognizable hex string representation.</summary>
    /// <remarks>
    /// The binary representation is stored as a string representing both signed and unsigned longs in log type A
    /// and stored as an actual signed long in log type B
    /// </remarks>
    public static string ToHexUid(this string uidBinaryAsString)
    {
        // in v4 it is the ulong binary value of the uid stored as text; sqlite only stores signed 64bit values natively 
        // in v6 it is the signed long value, which wraps to negative, but still stored as text;
        // in v7, it is the signed long value as a native integer
        long? signedUid = null;
        try
        {
            if (uidBinaryAsString.IsNotNullOrEmptyString())
            {
                if (uidBinaryAsString[0].Equals('-'))
                {
                    if (long.TryParse(uidBinaryAsString, out long alreadySigned))
                        signedUid = alreadySigned;
                }

                if (!signedUid.HasValue)
                {
                    if (ulong.TryParse(uidBinaryAsString, out ulong unsigned))
                        signedUid = (long)unsigned;
                }
            }

            if (signedUid.HasValue)
            {
                byte[] longAsBin = BitConverter.GetBytes(signedUid.Value);
                return string.Join("", longAsBin.Reverse().Select(b => $"{b:X2}"));
            }
        }
        catch { /* parse error */ }

        // still not parsed; but we don't want nulls
        return uidBinaryAsString;
    }
}
