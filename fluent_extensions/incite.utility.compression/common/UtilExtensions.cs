using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace contoso.utility.compression;

/// <summary>Common utility functions and extensions.</summary>
internal static class UtilExtensions
{
    const float _kib = 1024F;
    const string _hexChars = "0123456789ABCDEF";
    // first 32 control characters and 127 (DEL)
    readonly static char[] _ctlChars = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 127}.Select(b => (char)b).ToArray();

    /// <summary>Convert a file or memory size, in bytes, to a formatted display string.</summary>
    internal static string ToFormattedSizeDisplay(this in long bytes)
    {
        if (bytes < 0L)
            return "0 b";

        float sizeKb = bytes / _kib;
        float sizeMb = sizeKb / _kib;
        float sizeGb = sizeMb / _kib;
        return sizeGb >= 1 ? $"{sizeGb:F2} Gb"
                           : sizeMb >= 1 ? $"{sizeMb:F2} Mb"
                                         : sizeKb >= 1 ? $"{sizeKb:F1} Kb"
                                                       : $"{bytes} b";
    }

    /// <summary>Convert <paramref name="bytes"/> to its corresponding hex representation.</summary>
    public static bool TryConvertToHexString(in this ReadOnlySpan<byte> bytes, [NotNullWhen(true)] out string? hexString, bool spaced = false)
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
            hexString = null;
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

    /// <summary>True if the specified <see cref="ReadOnlySpan{Char}"/> is empty or consists only of white-space characters.</summary>
    public static bool IsEmptyOrWhitespace(this ReadOnlySpan<char> source) => source.IsEmpty || source.IsWhiteSpace();

    /// <summary>True if the specified <see cref="ReadOnlySpan{Char}"/> is not empty and does not consist only of white-space characters.</summary>
    public static bool IsNotEmptyOrWhitespace(this ReadOnlySpan<char> source) => !(source.IsEmpty || source.IsWhiteSpace());

    /// <summary>True if a collection is not null and has elements.</summary>
    public static bool IsNotNullOrEmpty<T>([NotNullWhen(true)] this IEnumerable<T>? source)
    {
        if (source is string str)
            return str.IsNotNullOrEmptyString();
        return (source != null) && (source.Any());
    }

    /// <summary>True if a collection is null or has no elements.</summary>
    public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this IEnumerable<T> source)
    {
        if (source is string str)
            return str.IsNullOrEmptyString();
        return (source == null) || (!source.Any());
    }

    /// <summary>True if the specified string is null, empty, or consists only of white-space characters.</summary>
    public static bool IsNullOrEmptyString([NotNullWhen(false)] this string? source) => string.IsNullOrWhiteSpace(source);

    /// <summary>True if a string is not null or empty or all blanks.</summary>
    public static bool IsNotNullOrEmptyString([NotNullWhen(true)] this string? source) => !string.IsNullOrWhiteSpace(source);

    // https://stackoverflow.com/a/864860/458354
    /// <summary>True if reference or nullable instance is null or if value instance is default.</summary>
    public static bool IsNullOrDefault<T>([NotNullWhen(false)] this T src) => EqualityComparer<T>.Default.Equals(src, default(T));

    /// <summary>
    /// Return only the characters in <paramref name="src"/> up to the first 'NUL' (0x00) character
    /// optionally also removing any non-printable ascii control characters
    /// </summary>
    public static string? UpToNullChars(this string? src, bool removeAsciiCtlChars = false)
    {
        if (string.IsNullOrEmpty(src))
            return src;
        bool hasNoEmbeddedNull = src.IndexOf('\0') < 0;
        if (removeAsciiCtlChars)
            // if no embedded null, invoke RemoveNonPrintable directly without allocating a new string; otherwise, there is a need for an intermediate string
            return hasNoEmbeddedNull ? RemoveAllCtlChars(src) : RemoveAllCtlChars(UpToNullChar(src));
        else
            // if not removing non-printables, simply return source unmodified if also no embedded null, otherwise there is a need for a new string
            return hasNoEmbeddedNull ? src : UpToNullChar(src);
    }

    /// <summary>Return only the characters in <paramref name="src"/> up to the first 'NUL' (0x00) character.</summary>
    public static string? UpToNullChar(this string? src)
    {
        if (string.IsNullOrEmpty(src))
            return src;
        int nulIdx = src.IndexOf('\0');
        return nulIdx < 0 ? src : src[..nulIdx];
    }

    /// <summary>Remove any ascii control characters (0-31, 127) from <paramref name="src"/>.</summary>
    public static string? RemoveAllCtlChars(this string? src)
    {
        if (string.IsNullOrEmpty(src))
            return src;
        if (src.IndexOfAny(_ctlChars) < 0)
            return src;
        else
        {
            var cleanChars = src.Where(c => !_ctlChars.Contains(c));
            return cleanChars.Any() ? new string(cleanChars.ToArray()) : string.Empty;
        }
    }

    /// <summary>Format <paramref name="ex"/> as a short message when handling expected exceptions.</summary>
    [return: NotNull]
    public static string AsShortMessage([AllowNull] this Exception ex) => ex == null ? string.Empty : $"[{ex.GetType().Name}]: {ex.Message}";
}
