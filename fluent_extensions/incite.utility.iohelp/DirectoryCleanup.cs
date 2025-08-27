namespace contoso.utility.iohelp;

/// <summary>Helper methods to clean or remove a directory either completely in one shot or iteratively as much as possible.</summary>
public static class DirectoryCleanup
{
    /// <summary>
    /// Attempt to remove <paramref name="directory"/> or empty as much as possible.
    /// No exception is thrown if encountered.
    /// </summary>
    /// <returns>True if <paramref name="directory"/> no longer exists and no errors were encountered. Otherwise, false.</returns>
    public static bool TryRemove(this DirectoryInfo? directory)
    {
        if (directory == null)
            return false;
        try
        {
            if (!directory.Exists)
                return true;
            return TryRemove(directory.FullName);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempt to empty <paramref name="directory"/> (leaving the directory itself) completely, or as much as possible.
    /// No exception is thrown if encountered.
    /// </summary>
    /// <returns>True if <paramref name="directory"/> is completely empty and no errors were encountered. Otherwise, false.</returns>
    public static bool TryEmpty(this DirectoryInfo? directory)
    {
        if (directory == null)
            return false;
        try
        {
            if (!directory.Exists)
                return false;
            return TryEmpty(directory.FullName);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempt to remove <paramref name="directoryPath"/> or empty as much as possible.
    /// No exception is thrown if encountered.
    /// </summary>
    /// <returns>True if <paramref name="directoryPath"/> no longer exists and no errors were encountered. Otherwise, false.</returns>
    public static bool TryRemove(string? directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            return false;
        try
        {
            DirectoryInfo dirInfo = new DirectoryInfo(Path.GetFullPath(directoryPath));
            if (!dirInfo.Exists)
                return true;

            // attempt to remove directory and all content in one operation
            try
            {
                dirInfo.Delete(recursive: true);
            }
            catch
            {
                /* intentionally ignoring typical exceptions: 'in-use', 'access-denied', 'bad-path' */
                TryEmpty(dirInfo.FullName);
            }

            // confirm is directory is completely removed or only as much as possible
            return dirInfo.Exists == false;
        }
        catch
        {
            /* intentionally ignoring typical exceptions: 'in-use', 'access-denied', 'bad-path' */
            return false;
        }
    }

    /// <summary>
    /// Attempt to empty <paramref name="directoryPath"/> (leaving the directory itself) completely, or as much as possible.
    /// No exception is thrown if encountered.
    /// </summary>
    /// <returns>True if <paramref name="directoryPath"/> is completely empty and no errors were encountered. Otherwise, false.</returns>
    public static bool TryEmpty(string? directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            return false;
        try
        {
            DirectoryInfo dirInfo = new DirectoryInfo(Path.GetFullPath(directoryPath));
            if (!dirInfo.Exists)
                return false;

            // iteratively remove each subdirectory 
            IEnumerable<DirectoryInfo> subdirectoryList = dirInfo.EnumerateDirectories();
            foreach (DirectoryInfo subDirInfo in subdirectoryList)
            {
                try
                {
                    subDirInfo.Delete(recursive: true);
                }
                catch { /* intentionally ignoring typical exceptions: 'in-use', 'access-denied', 'bad-path' */ }
            }

            // delete all files in the original (top-most) directory
            IEnumerable<FileInfo> fileList = dirInfo.EnumerateFiles();
            foreach (FileInfo file in fileList)
            {
                try
                {
                    if (!file.IsReadOnly)
                        file.Delete();
                }
                catch { /* intentionally ignoring typical exceptions: 'in-use', 'access-denied', 'bad-path' */ }
            }
            // confirm if directory is truly empty or only as empty as possible
            return !Directory.EnumerateFileSystemEntries(dirInfo.FullName).Any();
        }
        catch
        {
            /* intentionally ignoring typical exceptions: 'in-use', 'access-denied', 'bad-path' */
            return false;
        }
    }

}
