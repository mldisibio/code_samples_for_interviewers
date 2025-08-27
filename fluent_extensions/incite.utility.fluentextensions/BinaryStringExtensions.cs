using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace contoso.utility.fluentextensions;

/// <summary>Utility extensions around string representations of byte arrays.</summary>
public static class BinaryStringExtensions
{
    const string _hexChars = "0123456789ABCDEF";
    readonly static char[] _prefixChars = new[] { 'x', 'X' };

    /// <summary>Convert <paramref name="bytes"/> to its corresponding hex representation.</summary>
    public static bool TryConvertToHexString(in this ReadOnlySpan<byte> bytes, out string hexString, bool spaced = false)
    {
        if (bytes.IsEmpty)
        {
            hexString = string.Empty;
            return true;
        }
        int factor = spaced ? 3 : 2;
        StringBuilder sb = new StringBuilder((bytes.Length * factor) + 2);
        try
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                bool addSpace = spaced && i > 0;
                GetHexChars(bytes[i], addSpace, sb);
            }
            hexString = sb.ToString();
            return true;
        }
        catch
        {
            hexString = string.Empty;
            return false;
        }

        static void GetHexChars(byte b, bool addSpace, StringBuilder sb)
        {
            // if spaced and not first, yield a space
            if (addSpace)
                sb.Append(' ');
            // first four bits
            sb.Append(_hexChars[(int)((b >> 4) & 0xF)]);
            // second four bits
            sb.Append(_hexChars[(int)(b & 0xF)]);
        }
    }

    /// <summary>Convert the hex pairs from <paramref name="hexString"/> to their corresponding bytes, in the same order.</summary>
    public static bool TryConvertToByteArray(this string? hexString, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        try
        {
            if (string.IsNullOrEmpty(hexString))
                return false;
            var hexChars = GetHexChars(hexString);
            if (hexChars.Count() % 2 != 0)
                return false;

            bytes = hexChars.TakeBy(2)
                            .Select(pair => new string(pair.ToArray()))
                            .Select(hexPair => Convert.ToByte(hexPair, 16))
                            .ToArray();
            return true;
        }
        catch
        {
            return false;
        }

        static IEnumerable<char> GetHexChars(string hex) => hex.Skip(PrefixCharsToSkip(hex)).Where(c => char.IsLetterOrDigit(c));

        static int PrefixCharsToSkip(string hex) => hex.IndexOfAny(_prefixChars) + 1;
    }

    /// <summary>Return binary content (encrypted or unencrypted) as a base 64 string.</summary>
    public static string ConvertToBase64Text(this byte[]? binary)
    {
        if (binary == null)
            return string.Empty;
        return Convert.ToBase64String(binary);
    }

    /// <summary>Return binary content (encrypted or unencrypted) from a base 64 string.</summary>
    public static byte[] ConvertFromBase64Text(this string? text)
    {
        if (text.IsNullOrEmptyString())
            return Array.Empty<byte>();
        return Convert.FromBase64String(text);
    }

    /// <summary>
    /// Null safe helper to return <paramref name="length"/> elements of <paramref name="buffer"/> starting at <paramref name="start"/> as a <see cref="ReadOnlySpan{Byte}"/>.
    /// </summary>
    public static ReadOnlySpan<byte> GetSlice(this byte[]? buffer, int start, int length) => buffer == null ? Array.Empty<byte>() : buffer.AsSpan(start, length);

}
