using System.Diagnostics.CodeAnalysis;

namespace contoso.utility.ioabstracted;

internal static class PathUtils
{
    static readonly char[] _invalidFileNameChars = Path.GetInvalidFileNameChars()
                                                       .Where(c => !(c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar || c == Path.VolumeSeparatorChar))
                                                       .ToArray();

    /// <summary>True if <paramref name="path"/> has no invalid file name characters and has at least one alpha numeric characters or is unix 'root'.</summary>
    internal static bool AppearsValidPath([NotNullWhen(true)] this string? path, out string? error)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            error = "Path is null or empty";
            return false;
        }

        string trimmed = path.Trim();

        if (HasInvalidPathChars(trimmed))
        {
            error = "Path contains invalid characters";
            return false;
        }
        if (!(HasAlphaNumeric(trimmed) || IsUnixRoot(trimmed)))
        {
            error = "Path has no alphanumeric characters";
            return false;
        }
        if (!ZeroOrOneColon(trimmed))
        {
            error = "Path contains an invalid colon";
            return false;
        }
        error = null;
        return true;
    }

    /// <summary>True if <paramref name="path"/> contains system specific invalid file name characters with the exception of path separators.</summary>
    internal static bool HasInvalidPathChars(this string path) => path != null && path.IndexOfAny(_invalidFileNameChars) > -1;

    /// <summary>True if <paramref name="src"/>has at least one sensible alpha numeric character.</summary>
    internal static bool HasAlphaNumeric([NotNullWhen(true)] this string src) => src != null && src.Any(c => char.IsLetterOrDigit(c));

    static bool IsUnixRoot(ReadOnlySpan<char> path) => path.Length == 1 && path[0] == '/';
    
    static bool ZeroOrOneColon(ReadOnlySpan<char> path)
    {
        // if linux, simply ensure no colons in the path
        if (Path.VolumeSeparatorChar == Path.DirectorySeparatorChar)
            return path.IndexOf(':') == -1;
        // for windows, ensure only colon is for an absolute path e.g. c:\something
        int colonIdx = path.LastIndexOf(':');
        return colonIdx == -1 || colonIdx == 1;
    }

    /// <summary>Ensure <paramref name="path"/> ends with the system directory separator.</summary>
    internal static string EnsureTrailingSeparator(this string path)
        => string.IsNullOrEmpty(path)
           ? path
           : path[path.Length - 1] != Path.DirectorySeparatorChar
             ? string.Concat(path, Path.DirectorySeparatorChar)
             : path;

    /// <summary>Ensure <paramref name="path"/> ends with <paramref name="separator"/>.</summary>
    internal static string EnsureTrailingSeparator(this string path, char separator)
        => string.IsNullOrEmpty(path)
           ? path
           : path[path.Length - 1] != separator
             ? string.Concat(path, separator)
             : path;

    // https://stackoverflow.com/a/864860/458354
    /// <summary>True if reference or nullable instance is null or if value instance is default.</summary>
    internal static bool IsNullOrDefault<T>([NotNullWhen(false)] this T src) => EqualityComparer<T>.Default.Equals(src, default(T));

}
