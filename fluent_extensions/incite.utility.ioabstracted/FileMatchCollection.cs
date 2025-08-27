using System.Collections;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;

namespace contoso.utility.ioabstracted;

/// <summary>An enumerable collection of files or storage keys matching a given criteria.</summary>
/// <remarks>
/// By using the Microsoft.Extensions.FileProviders abstraction, this directory search
/// should be usable for both file system searches and S3/cloud storage searches.
/// </remarks>
public class FileMatchCollection : IEnumerable<IFileInfo>
{
    readonly static NullFileProvider _emptyProvider = new NullFileProvider();
    readonly static Predicate<IFileInfo> FileMatchDefault = (item => !item.IsDirectory);
    readonly static Predicate<IFileInfo> DirectoryMatchDefault = (item => item.IsDirectory);

    readonly IFileProvider _fileProvider;
    readonly Predicate<IFileInfo> _fileFilter;
    readonly Predicate<IFileInfo> _directoryFilter;
    readonly SearchOption _recurseOption;

    /// <summary>
    /// Creates an enumerable set of paths matching <paramref name="fileFilter"/> in <paramref name="fileProvider"/>
    /// with an optional <paramref name="recurseOption"/> and <paramref name="directoryFilter"/>.
    /// </summary>
    /// <param name="fileProvider">
    /// An <see cref="IFileProvider"/> initialized with the absolute path to the directory or storage path in which the search starts.
    /// Any <paramref name="directoryFilter"/> is not applied to this start path. Its contents will be included in the search by default.
    /// </param>
    /// <param name="fileFilter">
    /// A predicate against which each path, wrapped as <see cref="IFileInfo"/>, will be evaluated.
    /// This predicate can be as complex as needed, can make use of regular expressions, and can evaluate the items full path.
    /// </param>
    /// <param name="directoryFilter">
    /// A predicate against which each <see cref="IFileInfo"/> result with <see cref="IFileInfo.IsDirectory"/> set true, will be evaluated.
    /// Once a directory does not meet the conditions of the predicate, it and its subdirectories will be eliminated from the search tree.
    /// Do not use this predicate if the intent is to examine all directories at all levels.
    /// </param>
    /// <param name="recurseOption">
    /// Specify whether to search just <paramref name="fileProvider"/> root or all the subpaths below it.
    /// Default is to seach all subpaths.
    /// </param>
    public FileMatchCollection(IFileProvider fileProvider,
                               Predicate<IFileInfo>? fileFilter = null,
                               Predicate<IFileInfo>? directoryFilter = null,
                               SearchOption recurseOption = SearchOption.AllDirectories)
    {
        _fileProvider = PathUtils.IsNullOrDefault(fileProvider) ? _emptyProvider : fileProvider;
        _fileFilter = fileFilter ?? FileMatchDefault;
        _directoryFilter = directoryFilter ?? DirectoryMatchDefault;
        _recurseOption = recurseOption;
    }

    /// <summary>
    /// Iterates directory and returns matching files.
    /// If search is recursive, will iterate all directories, but method itself only returns matching files.
    /// </summary>
    /// <remarks>Assumption is that S3 or other non-physical providers will iterate only one flat directory.</remarks>
    public IEnumerator<IFileInfo> GetEnumerator()
    {
        foreach (IFileInfo item in EnumerateSubPath(_fileProvider, string.Empty))
            yield return item;
    }

    IEnumerable<IFileInfo> EnumerateSubPath(IFileProvider fileProvider, string subpath)
    {
        // using Stack appears to give less mem better perf than recursive for deep hierarchy
        var nextDirectories = new Stack<string>(1024);
        nextDirectories.Push(subpath);

        while (nextDirectories.TryPop(out string? currentPath))
        {
            IDirectoryContents dirContents = TryGetDirectoryContents(fileProvider, currentPath);
            if (dirContents.Exists)
            {
                // UnauthorizedAccessException is thrown here, while enumerating, even if 'dirContents.Exists' succeeded
                foreach (IFileInfo item in dirContents)
                {
                    if (item.IsDirectory)
                    {
                        // if search is recursive, add the 'relative' directory path to the stack
                        if (_recurseOption == SearchOption.AllDirectories && DirectoryIsEligible(item, _directoryFilter))
                        {
                            // 'Name' is the name of the file or directory, not including any path.
                            // append it to the current subpath (which would represent its parent directory also as a relative path)
                            var (NoError, NextSubPath) = TryGetNextSubPath(currentPath, item.Name);
                            if (NoError)
                                nextDirectories.Push(NextSubPath!);
                        }
                    }
                    else
                    {
                        // for a file item, return it if it passes the predicate
                        if (FileIsEligible(item, _fileFilter))
                            yield return item;
                    }
                }
            }
        }

        static IDirectoryContents TryGetDirectoryContents(IFileProvider fileProvider, string subpath)
        {
            try
            {
                var contents = fileProvider.GetDirectoryContents(subpath);
                // enumerating the contents is what actually throws UnauthorizedAccessException if caller does not have read permissions
                // both 'GetDirectoryContents' and 'Exists' can succeed even w/o permissions
                if (contents.Any())
                    return contents;
            }
            catch (UnauthorizedAccessException) { Console.WriteLine($"Access Denied: {subpath}"); }
            catch (Exception) { }

            return NotFoundDirectoryContents.Singleton;
        }

        static (bool NoError, string? NextSubPath) TryGetNextSubPath(string current, string? next)
        {
            if (string.IsNullOrWhiteSpace(next))
                return (false, null);
            if (string.Equals(next, ".") || string.Equals(next, ".."))
                return (false, null);
            // for now, we know we are on either filesystem or S3;
            // if S3, we treat the file system flat, no subdirectories;
            // so 'Path.Join' is appropriate here; 
            try { return (true, Path.Join(current, next)); }
            catch { return (false, null); }
        }
    }

    static bool FileIsEligible(IFileInfo item, Predicate<IFileInfo> fileFilter)
    {
        try
        {
            if (PathUtils.IsNullOrDefault(item))
                return false;
            if (string.IsNullOrWhiteSpace(item.PhysicalPath))
                return false;
            if (item.IsDirectory)
                return false;
            if (!item.Exists)
                return false;
            // leave handling of zero length files to caller
            //if (item.Length == 0)
            //    return false;
            if (!IsEligibleIfPhysicalFile(item))
                return false;
            // apply caller predicate
            return fileFilter(item);
        }
        catch
        {
            // could be exception in caller predicate or possibly invalid path error
            return false;
        }
    }

    static bool IsEligibleIfPhysicalFile(IFileInfo item)
    {
        if (item is PhysicalFileInfo diskFile)
        {
            FileInfo info;
            info = new FileInfo(diskFile.PhysicalPath);
            if (info == null)
                return false;
            if (!info.Exists)
                return false;
            // opting to ignore hidden files for our purposes
            if ((info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                return false;
            // do not follow symbolic links
            if ((info.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                return false;
        }
        // returns true also if simply not a physical file
        return true;
    }

    static bool DirectoryIsEligible(IFileInfo item, Predicate<IFileInfo> directoryFilter)
    {
        try
        {
            if (PathUtils.IsNullOrDefault(item))
                return false;
            if (string.IsNullOrWhiteSpace(item.PhysicalPath))
                return false;
            if (!item.IsDirectory)
                return false;
            if (!item.Exists)
                return false;
            if (!IsEligibleIfPhysicalDirectory(item))
                return false;
            // apply caller predicate
            return directoryFilter(item);
        }
        catch
        {
            // could be exception in caller predicate or possibly invalid path error
            return false;
        }
    }

    static bool IsEligibleIfPhysicalDirectory(IFileInfo item)
    {
        if (item is PhysicalDirectoryInfo diskDir)
        {
            DirectoryInfo info = new DirectoryInfo(diskDir.PhysicalPath);
            if (info == null)
                return false;
            if (!info.Exists)
                return false;
            // opting to ignore hidden directories for our purposes
            if ((info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                return false;
            // do not follow symbolic links
            if ((info.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                return false;
            // has 'Directory' attribute and permissions can be read
            if ((info.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                return true;

            // well, this only works for windows. boo.
            /*
            if ((info.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
            {
                try { _ = FileSystemAclExtensions.GetAccessControl(info); return true; }
                catch (UnauthorizedAccessException) { Console.WriteLine($"Access Denied: {info.FullName}"); return false; }
            }
            */
        }
        // returns true also if simply not a physical directory
        return true;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
