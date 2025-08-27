using System.Collections;

/*
 * Acknowledgment: based upon concept presented at https://stackoverflow.com/a/13130054/458354
 *                 but also significantly modified from the original
 */

namespace contoso.utility.fluentextensions;

/// <summary>
/// Enumerate files and folders in a specified directory, attempting to skip files and folders
/// which throw an <see cref="UnauthorizedAccessException"/> or other <see cref="IOException"/>	
/// </summary>
/// <remarks>
/// If used from within a docker container, the instance can search the host file system
/// if the search directory is mounted as a volume. However, the full path of each match
/// will be relative to the root volume within docker to which the host directory is bound.
/// </remarks>
public class FileMatchEnumerable : IEnumerable<string>
{

    readonly DirectoryInfo? _directory;
    readonly Predicate<FileInfo> _fileFilter;
    readonly Predicate<DirectoryInfo> _directoryFilter;
    readonly SearchOption _recurseOption;

    /// <summary>
    /// Creates an enumerable set of file paths matching <paramref name="fileFilter"/> in <paramref name="startPath"/>
    /// with an optional <paramref name="recurseOption"/> and <paramref name="directoryFilter"/>.
    /// </summary>
    /// <param name="startPath">
    /// Full path to the directory in which the search starts.
    /// <paramref name="directoryFilter"/> is not applied to this directory and its files will be included in the search by default.
    /// </param>
    /// <param name="fileFilter">
    /// A predicate against which each file, wrapped as <see cref="FileInfo"/>, will be evaluated.
    /// This predicate can be as complex as needed, can make use of regular expressions, and can evaluate the file's full path.
    /// </param>
    /// <param name="directoryFilter">
    /// A predicate against which each directory, wrapped as <see cref="DirectoryInfo"/>, will be evaluated.
    /// Once a directory does not meet the conditions of the predicate, it and its subdirectories will be eliminated from the search tree.
    /// Do not use this predicate if the intent is to examine all directories at all levels.
    /// </param>
    /// <param name="recurseOption">
    /// Specify whether to search just <paramref name="startPath"/> or all the subdirectories below it.
    /// Default is to seach all directories.
    /// </param>
    public FileMatchEnumerable(string startPath,
                               Predicate<FileInfo>? fileFilter = null,
                               Predicate<DirectoryInfo>? directoryFilter = null,
                               SearchOption recurseOption = SearchOption.AllDirectories)
    {
        _directory = Normalize(startPath);
        _fileFilter = fileFilter ?? (_ => true);
        _directoryFilter = directoryFilter ?? (_ => true);
        _recurseOption = recurseOption;
    }

    /// <summary>
    /// Enumerate all file paths matching <paramref name="fileFilter"/> in <paramref name="startPath"/>
    /// optionally specifying a <paramref name="recurseOption"/> and <paramref name="directoryFilter"/>.
    /// </summary>
    /// <param name="startPath">
    /// Full path to the directory in which the search starts.
    /// <paramref name="directoryFilter"/> is not applied to this directory and its files will be included in the search by default.
    /// </param>
    /// <param name="fileFilter">
    /// A predicate against which each file, wrapped as <see cref="FileInfo"/>, will be evaluated.
    /// This predicate can be as complex as needed, can make use of regular expressions, and can evaluate the file's full path.
    /// </param>
    /// <param name="directoryFilter">
    /// A predicate against which each directory, wrapped as <see cref="DirectoryInfo"/>, will be evaluated.
    /// Once a directory does not meet the conditions of the predicate, it and its subdirectories will be eliminated from the search tree.
    /// Do not use this predicate if the intent is to examine all directories at all levels.
    /// </param>
    /// <param name="recurseOption">
    /// Specify whether to search just <paramref name="startPath"/> or all the subdirectories below it.
    /// Default is to seach all directories.
    /// </param>
    public static FileMatchEnumerable Enumerate(string startPath,
                                                Predicate<FileInfo>? fileFilter = null,
                                                Predicate<DirectoryInfo>? directoryFilter = null,
                                                SearchOption recurseOption = SearchOption.AllDirectories)
    {
        return new FileMatchEnumerable(startPath, fileFilter, directoryFilter, recurseOption);
    }

    /// <summary>Returns the enumerator that iterates through matching directories and files.</summary>
    public IEnumerator<string> GetEnumerator()
    {
        if (_directory == null)
            yield break;

        // enumerate matching files in current directory 
        IEnumerable<string> matchedFiles = Enumerable.Empty<string>();
        try
        {
            matchedFiles = Directory.EnumerateFiles(_directory.FullName).Where(file => FileIsEligible(file));
        }
        catch
        {
            // an exception will force this directory and its files to be ignored, but the overall iteration will continue
            yield break;
        }

        // return each matched file
        foreach (string fileName in matchedFiles)
            yield return fileName;

        if (_recurseOption == SearchOption.AllDirectories)
        {
            // enumerate the next level of child subdirectories
            IEnumerable<string> matchedSubDirectories = Enumerable.Empty<string>();
            try
            {
                matchedSubDirectories = Directory.EnumerateDirectories(_directory.FullName).Where(subDir => DirectoryIsEligible(subDir));
            }
            catch
            {
                // an exception will force the subdirectory and its files to be ignored, but the overall iteration will continue
                yield break;
            }
            foreach (string nextDir in matchedSubDirectories)
            {
                // create an enumerator over each next level subdirectory
                var nextEnumerator = new FileMatchEnumerable(nextDir, _fileFilter, _directoryFilter, _recurseOption);
                // and enumerate the file matches for each
                foreach (string fileMatch in nextEnumerator)
                    yield return fileMatch;
            }
        }
    }

    static DirectoryInfo? Normalize(string? path)
    {
        if (path.IsNullOrEmptyString())
            return null;
        try
        {
            DirectoryInfo dirInfo = new DirectoryInfo(Path.GetFullPath(path));
            if (dirInfo.Exists)
                return dirInfo;
        }
        catch { }
        // if the path is invalid or does not exist, setting it to null simply stops the enumerator
        return null;
    }

    /// <summary>Skip hidden directories and symbolic links</summary>
    bool DirectoryIsEligible(string directoryName)
    {
        if (directoryName.IsNullOrEmptyString())
            return false;
        try
        {
            // this will throw if the path is too long or has illegal characters (as in 'oasis files' grrrr)
            var dirInfo = new DirectoryInfo(directoryName);
            if ((dirInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                return false;
            if ((dirInfo.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                return false;
            if ((dirInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                return _directoryFilter(dirInfo);
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Skip hidden files or file links/shortcuts.</summary>
    bool FileIsEligible(string? fileName)
    {
        if (fileName.IsNullOrEmptyString())
            return false;
        try
        {
            // this will throw if the path is too long or has illegal characters (as in 'oasis files' grrrr)
            var fileInfo = new FileInfo(fileName);
            if (fileInfo == null)
                return false;
            if (!fileInfo.Exists)
                return false;
            if ((fileInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                return false;
            if ((fileInfo.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                return false;
            //if ((src.Attributes & FileAttributes.Normal) == FileAttributes.Normal)
            //	return fileFilter(src);
            return _fileFilter(fileInfo);
        }
        catch
        {
            return false;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
