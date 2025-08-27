using System.Diagnostics.CodeAnalysis;

namespace contoso.utility.iohelp;

/// <summary>Fluent extensions over common path or I/O task.</summary>
public static class FileSystemHelp
{
    const float _kib = 1024F;
    readonly static char[] _pathSeparators = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

    /// <summary>Fluent extension to create any necessary parent directory structure for the given file path.</summary>
    /// <returns>The original value of <paramref name="filePath"/>.</returns>
    [return: NotNull]
    public static string WithParentDirectoryCreation(this string? filePath)
    {
        if (EnsureDirectoryExistsFor(filePath, out string? _))
            return filePath;
        else
            throw new IOException(message: $"Attempting to ensure directory structure exists for [{filePath}]");
    }

    /// <summary>Ensure the full directory path to <paramref name="filePath"/> is created.</summary>
    public static bool EnsureDirectoryExistsFor([NotNullWhen(true)] string? filePath, [NotNullWhen(true)] out string? directoryPath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(paramName: nameof(filePath));
        try
        {
            string fullPath = Path.GetFullPath(filePath);
            directoryPath = Path.GetDirectoryName(fullPath);

            // empty if path does not contain directory information; null if path represents a root directory only with no file
            if (string.IsNullOrEmpty(directoryPath))
                return false;

            // (creates only if needed)
            Directory.CreateDirectory(directoryPath);

            return Directory.Exists(directoryPath);
        }
        catch (Exception ex)
        {
            throw new IOException(message: $"Attempting to ensure directory structure exists for [{filePath}]", innerException: ex);
        }
    }

    /// <summary>
    /// Removes leading directory separator characters from a relative path.
    /// Whitespace is treated as significant and not ignored.
    /// This allows <c>Path.Combine</c> to work correctly when concatenating a root with a relative path
    /// (the relative path cannot start with a directory separator).
    /// </summary>
    /// <param name="relativePath">A string representing a relative file path.</param>
    /// <returns>The <paramref name="relativePath"/>exactly as is, less any first character directory separator.</returns>
    public static string? WithoutInitialBackslash(this string? relativePath)
    {
        if (String.IsNullOrWhiteSpace(relativePath))
            return relativePath;
        return relativePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    /// <summary>Convert a file or memory size, in bytes, to a formatted display string.</summary>
    [return: NotNull]
    public static string ToFormattedSizeDisplay(this in long bytes)
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

    /// <summary>Return the set of all segments in a file or directory path.</summary>
    /// <param name="filePath">The file or directory path to split into segments.</param>
    /// <param name="toLower">True to return each segment converted to lower case, for case-insensitive comparisons.</param>
    [return: NotNull]
    public static string[] PathSegments(this string? filePath, bool toLower = false)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return Array.Empty<string>();
        string[] segments = filePath.Split(_pathSeparators, StringSplitOptions.RemoveEmptyEntries);

        return toLower ? makeLowerCase(segments) : segments;

        static string[] makeLowerCase(string[] src)
        {
            for (int i = 0; i < src.Length; i++)
                src[i] = src[i].ToLower();
            return src;
        }
    }

    /// <summary>Return the set of all segments in a file or directory path.</summary>
    /// <param name="src">A <see cref="FileSystemInfo"/> object from which the full name will be split into segments.</param>
    /// <param name="toLower">True to return each segment converted to lower case, for case-insensitive comparisons.</param>
    public static string[] PathSegments(this FileSystemInfo src, bool toLower = false) => PathSegments(src?.FullName, toLower);

}
