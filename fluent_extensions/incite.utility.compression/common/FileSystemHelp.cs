using System.Diagnostics.CodeAnalysis;

namespace contoso.utility.compression;

internal static class FileSystemHelp
{
    /// <summary>Fluent extension to create any necessary parent directory structure for the given file path.</summary>
    /// <returns>The original value of <paramref name="filePath"/>.</returns>
    [return: NotNull]
    internal static string WithParentDirectoryCreation(this string? filePath)
    {
        if(EnsureDirectoryExistsFor(filePath, out string? _))
            return filePath;
        else
            throw new IOException(message: $"Attempting to ensure directory structure exists for [{filePath}]");
    }

    /// <summary>Ensure the full directory path to <paramref name="filePath"/> is created.</summary>
    internal static bool EnsureDirectoryExistsFor([NotNullWhen(true)]string? filePath, [NotNullWhen(true)]out string? directoryPath)
    {
        if (filePath.IsNullOrEmptyString())
            throw new ArgumentNullException(paramName: nameof(filePath));
        try
        {
            string fullPath = Path.GetFullPath(filePath);
            directoryPath = Path.GetDirectoryName(fullPath);

            // empty if path does not contain directory information; null if path represents a root directory only with no file
            if (directoryPath.IsNullOrEmptyString())
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
}
