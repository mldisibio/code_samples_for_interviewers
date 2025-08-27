using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace contoso.utility.fluentextensions;

/// <summary>Extensions enabling fluent syntax around simple string checks.</summary>
public static class StringExtensions
{
    // first 32 control characters and 127 (DEL)
    readonly static char[] _ctlChars = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 127}.Select(b => (char)b).ToArray();
    // first 32 control characters except for \n\r\t; also includes 127 (DEL)
    readonly static char[] _ctlCharsLessLineOrTab = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 11, 12, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 127}.Select(b => (char)b).ToArray();
    readonly static char[] _newLineChars = new char[] { '\n', '\r' };
    readonly static char[] _newLineAndTabChars = new char[] { '\n', '\r', '\t' };

    /// <summary>True if the specified <see cref="ReadOnlySpan{Char}"/> is empty or consists only of white-space characters.</summary>
    public static bool IsEmptyOrWhitespace(this ReadOnlySpan<char> source) => source.IsEmpty || source.IsWhiteSpace();

    /// <summary>True if the specified <see cref="ReadOnlySpan{Char}"/> is not empty and does not consist only of white-space characters.</summary>
    public static bool IsNotEmptyOrWhitespace(this ReadOnlySpan<char> source) => !(source.IsEmpty || source.IsWhiteSpace());

    /// <summary>True if the specified string is null, empty, or consists only of white-space characters.</summary>
    public static bool IsNullOrEmptyString([NotNullWhen(false)] this string? source) => string.IsNullOrWhiteSpace(source);

    /// <summary>True if a string is not null or empty or all blanks.</summary>
    public static bool IsNotNullOrEmptyString([NotNullWhen(true)] this string? source) => !string.IsNullOrWhiteSpace(source);

    /// <summary>
    /// If the supplied string is null, empty, or only spaces, returns null.
    /// Otherwise, returns the string with both leading and trailing white-space characters removed.
    /// </summary>
    /// <param name="source">The source string.</param>
    public static string? NullIfEmptyElseTrimmed(this string? source) => NullIfEmptyElseTrimmed(source, false);

    /// <summary>
    /// If the supplied string is null, empty, or only spaces, returns null.
    /// Otherwise, returns the string with trailing white-space characters removed,
    /// and also leading white-space removed when <paramref name="trimEndOnly"/> is false.
    /// </summary>
    /// <param name="source">The source string.</param>
    /// <param name="trimEndOnly">True to trim only trailing white-space characters.</param>
    public static string? NullIfEmptyElseTrimmed(this string? source, bool trimEndOnly = false)
    {
        if (source == null)
            return source;
        if (string.IsNullOrWhiteSpace(source))
            return null;
        // framework will NOT allocate a new string if it does not have whitespace to trim
        return trimEndOnly ? source.TrimEnd() : source.Trim();
    }

    /// <summary>
    /// If the supplied string is null, empty, or only spaces, returns <c>String.Empty</c>.
    /// Otherwise, returns the string with trailing white-space characters removed,
    /// and also leading white-space removed when <paramref name="trimEndOnly"/> is false.
    /// </summary>
    /// <param name="source">The source string.</param>
    /// <param name="trimEndOnly">True to trim only trailing white-space characters.</param>
    [return: NotNull]
    public static string EmptyIfNullElseTrimmed(this string? source, bool trimEndOnly = false)
    {
        if (string.Equals(source, String.Empty))
            return source!;

        if (string.IsNullOrWhiteSpace(source))
            return string.Empty;
        // framework will NOT allocate a new string if it does not have whitespace to trim
        return trimEndOnly ? source.TrimEnd() : source.Trim();
    }

    /// <summary>Shortcut to <c>String.Equals(source, other, StringComparison.Ordinal)</c> returning true if arguments are equal, case and whitespace sensitive.</summary>
    /// <remarks>Does not modify either string at all. Returns true if both are null.</remarks>
    public static bool IsExactlyEqualTo(this string? source, string? other) => string.Equals(source, other, StringComparison.Ordinal);

    /// <summary>Shortcut to <c>String.Equals(source.Trim(), other.Trim(), StringComparison.OrdinalIgnoreCase))</c> - case and trailing whitespace insensitive.</summary>
    /// <remarks>Does not modify either string at all. Returns true if both are null but false if only one is null, even if other is empty.</remarks>
    public static bool IsAcceptedlyEqualTo(this string? source, string? other)
    {
        if (source == null || other == null)
            return source == null && other == null;
        if (string.Equals(source, other))
            return true;
        // compare as if both strings are trimmed, without actually allocating new strings
        return source.AsSpan().Trim().CompareTo(other.AsSpan().Trim(), StringComparison.OrdinalIgnoreCase) == 0;
    }

    /// <summary>
    /// Return only the characters in <paramref name="src"/> up to the first 'NUL' (0x00) character
    /// optionally also removing any non-printable ascii control characters
    /// </summary>
    public static string? UpToNullChars(this string? src, bool removeAllCtlChars = false)
    {
        if (string.IsNullOrEmpty(src))
            return src;
        bool hasNoEmbeddedNull = src.IndexOf('\0') < 0;
        if (removeAllCtlChars)
            // if no embedded null, invoke RemoveNonPrintable directly without allocating a new string; otherwise, there is a need for an intermediate string
            return hasNoEmbeddedNull ? RemoveAllCtrlChars(src) : RemoveAllCtrlChars(UpToNullChar(src));
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
    /// <remarks>Removes newlines and tab, so replace those first (e.g. with space) if you want to flatten with spacing.</remarks>
    public static string? RemoveAllCtrlChars(this string? src)
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

    /// <summary>Remove any ascii control characters except tab, LF, CR from <paramref name="src"/>.</summary>
    public static string? RemoveNonPrintableCtrlChars(this string? src)
    {
        if (string.IsNullOrEmpty(src))
            return src;
        if (src.IndexOfAny(_ctlCharsLessLineOrTab) < 0)
            return src;
        else
        {
            var cleanChars = src.Where(c => !_ctlCharsLessLineOrTab.Contains(c));
            return cleanChars.Any() ? new string(cleanChars.ToArray()) : string.Empty;
        }
    }

    /// <summary>Replace one or more consecutive new lines (\n or \r\n) with one space. Optionally do same for \t.</summary>
    public static string? NewlinesToSpace(this string? src, bool includeTabs = false)
    {
        if (string.IsNullOrEmpty(src))
            return src;
        if (includeTabs)
        {
            if (src.IndexOfAny(_newLineAndTabChars) < 0)
                return src;
            else
                return new string(src.Select(c => ReplaceIfNewLineOrTabChar(c)).ToArray());
        }
        else
        {
            if (src.IndexOfAny(_newLineChars) < 0)
                return src;
            else
                return new string(src.Select(c => ReplaceIfNewLineChar(c)).ToArray());
        }

        static char ReplaceIfNewLineChar(char c) => c.Equals(_newLineChars[0]) || c.Equals(_newLineChars[1]) ? ' ' : c;
        static char ReplaceIfNewLineOrTabChar(char c) => c.Equals(_newLineAndTabChars[0]) || c.Equals(_newLineAndTabChars[1]) || c.Equals(_newLineAndTabChars[2]) ? ' ' : c;
    }
}

