using System.Diagnostics.CodeAnalysis;

namespace contoso.utility.ioabstracted;

/// <summary>Path extensions but not dependent on System.IO.</summary>
public static class IOPathHelp
{
    readonly static char[] _pathSeparators = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

    /// <summary>Return the set of all segments in a key or path wrapped by <see cref="IOPath"/>.</summary>
    /// <param name="src">A <see cref="IOPath"/> object whose will be split into segments if directory separators are found.</param>
    /// <param name="toLower">True to return each segment converted to lower case, for case-insensitive comparisons.</param>
    public static string[] Segments(this IOPath src, bool toLower = false) => IOSegments(src.Value, toLower);

    /// <summary>Return the set of all segments in a string representing a storage path whose separator is forward slash or backslash.</summary>
    /// <param name="path">The path or key to split into segments.</param>
    /// <param name="toLower">True to return each segment converted to lower case, for case-insensitive comparisons.</param>
    [return: NotNull]
    public static string[] IOSegments(this string? path, bool toLower = false)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Array.Empty<string>();
        string[] segments = path.Split(_pathSeparators, StringSplitOptions.RemoveEmptyEntries);

        return toLower ? makeLowerCase(segments) : segments;

        static string[] makeLowerCase(string[] src)
        {
            for (int i = 0; i < src.Length; i++)
                src[i] = src[i].ToLower();
            return src;
        }
    }

    /// <summary>Return the path up to the last directory separator exclusive, or an empty string if no parent found or parent is root.</summary>
    [return: NotNull]
    public static string GetParentPath(this string? path)
    {
        if (path == null || string.IsNullOrWhiteSpace(path) || path.Length <= 1)
            return string.Empty;
        try
        {
            string? parent = Path.GetDirectoryName(path);
            return parent ?? string.Empty;
        }
        catch
        {
            try
            {
                int lastSep = path.LastIndexOfAny(_pathSeparators);
                return lastSep > 0 ? path[..lastSep] : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
