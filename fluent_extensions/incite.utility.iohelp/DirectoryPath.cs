
using System.Diagnostics.CodeAnalysis;

namespace contoso.utility.iohelp;

/// <summary>Wraps a string as a strongly typed directory path.</summary>
public struct DirectoryPath : IEquatable<DirectoryPath>
{
    readonly int? _hash;

    /// <summary>Initialize with a string representing a directory path.</summary>
    /// <param name="path">A string representing a directory path.</param>
    /// <param name="throwIfInvalid">True to throw any invalid path exception immediately (default). False to not throw exception and set <see cref="IsValid"/> to false.</param>
    public DirectoryPath(string? path, bool throwIfInvalid = true)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            FullPath = string.Empty;
            IsValid = false;
            Error = $"{nameof(path)} is empty";
            _hash = 0;
            if (throwIfInvalid)
                throw new ArgumentException(Error);
        }
        else
        {
            try
            {
                DirectoryInfo info = new DirectoryInfo(Path.GetFullPath(path));
                FullPath = info.FullName;
                IsValid = true;
                Error = null;
                _hash = FullPath.ToLowerInvariant().GetHashCode();
            }
            catch (Exception pathEx)
            {
                if (throwIfInvalid)
                    throw;

                FullPath = path;
                IsValid = false;
                Error = pathEx.Message;
                _hash = 0;
            }
        }
    }

    /// <summary>Fully qualified location of submitted path.</summary>
    public string FullPath { get; init; }

    /// <summary>True if submitted string was parsed as a directory path without error. Does not mean directory exists.</summary>
    public bool IsValid { get; private set; }

    /// <summary>Any exception message when parsing the string submitted as a path, or null if no exception was encountered.</summary>
    public string? Error { get; private set; }

    /// <summary>Wraps check for existence in a try catch.</summary>
    public bool Exists()
    {
        try { return TryGetDirectoryInfo(out DirectoryInfo? info) && info.Exists; }
        catch { return false; }
    }

    /// <summary>Wraps check for existence and file count in a try catch.</summary>
    /// <returns>Null if directory does not exist or throws an error, zero if exists but empty, otherwise the file count.</returns>
    public (bool Exists,int? FileCount) HasFiles(string pattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        try
        {
            if (TryGetDirectoryInfo(out DirectoryInfo? info) && info.Exists)
            {
                int matchCount = info.EnumerateFiles(searchPattern: pattern ?? "*", searchOption).Count();
                return new(true, matchCount);
            }
        }
        catch { }
        return new(false, null);
    }

    /// <summary>Wraps check for existence and file count in a try catch.</summary>
    /// <returns>Null if directory does not exist or throws an error, true if exists but empty, otherwise false.</returns>
    public (bool Exists, bool? IsEmpty) IsEmpty() 
    {
        var dir = HasFiles();
        bool? isEmpty = dir.Exists ? dir.FileCount.GetValueOrDefault() == 0 : null;
        return new(dir.Exists, isEmpty);
    }

    /// <summary>Return the path as a <see cref="DirectoryInfo"/> instance. Any exception is thrown.</summary>
    public DirectoryInfo AsDirectoryInfo() => new DirectoryInfo(FullPath);

    /// <summary>Returns true if the path is valid and a <see cref="DirectoryInfo"/> instance is created from it.</summary>
    public bool TryGetDirectoryInfo([NotNullWhen(true)] out DirectoryInfo? directoryInfo)
    {
        if (!IsValid)
        {
            directoryInfo = null;
            return false;
        }
        try
        {
            directoryInfo = new DirectoryInfo(FullPath);
            return true;
        }
        catch(Exception ex)
        {
            directoryInfo = null;
            IsValid = false;
            Error = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Attempt to remove the directory represented by this instance or empty as much as possible.
    /// No exception is thrown if encountered.
    /// </summary>
    /// <returns>True if the directory no longer exists and no errors were encountered. Otherwise, false.</returns>
    public bool TryRemove()
    {
        if (IsValid && TryGetDirectoryInfo(out DirectoryInfo? directory))
        {
            if (!directory.Exists)
                return true;
            return DirectoryCleanup.TryRemove(directory);
        }
        return false;
    }

    /// <summary>
    /// Attempt to completely empty the directory represented by this instance (leaving the directory itself), or as much as possible.
    /// No exception is thrown if encountered.
    /// </summary>
    /// <returns>True if the directory is completely empty and no errors were encountered. Otherwise, false.</returns>
    public bool TryEmpty()
    {
        if (IsValid && TryGetDirectoryInfo(out DirectoryInfo? directory))
        {
            if (!directory.Exists)
                return false;
            return DirectoryCleanup.TryEmpty(directory);
        }
        return false;
    }

    /// <summary>Return a new instance of <see cref="DirectoryPath"/> representing <paramref name="segments"/> appended to the current path, if valid.</summary>
    public DirectoryPath Append(params string[] segments) => IsValid ? new DirectoryPath(Path.Join(segments.Prepend(FullPath).ToArray())) : default;

    /// <summary>Return a new instance of <see cref="FilePath"/> representing <paramref name="fileName"/> appended to the current path, if both are valid.</summary>
    public FilePath AppendFileName(string? fileName) => IsValid  && (new FilePath(fileName)).IsValid ? new FilePath(Path.Join(FullPath, fileName)) : default;

    /// <inheritdoc/>
    public override string ToString() => FullPath ?? string.Empty;

    /// <inheritdoc/>
    public override int GetHashCode() => _hash.GetValueOrDefault();

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj) => (obj is DirectoryPath other) && Equals(other);

    /// <summary>True if both fully qualified absolute paths valid and exactly equal.</summary>
    public bool Equals(DirectoryPath other) => string.Equals(FullPath, other.FullPath, StringComparison.InvariantCulture) && string.Equals(Error, other.Error, StringComparison.InvariantCulture);

    /// <summary>True if both fully qualified absolute paths valid and equal, case-insensitive.</summary>
    public bool EqualsIgnoringCase(DirectoryPath other) => string.Equals(FullPath, other.FullPath, StringComparison.InvariantCultureIgnoreCase) && string.Equals(Error, other.Error, StringComparison.InvariantCultureIgnoreCase);

    /// <summary>True if both fully qualified absolute paths valid and exactly equal.</summary>
    public static bool operator ==(DirectoryPath left, DirectoryPath right) => left.Equals(right);

    /// <summary>True if either path is invalid or both paths are not exactly equal.</summary>
    public static bool operator !=(DirectoryPath left, DirectoryPath right) => !(left == right);

    /// <summary>Implicit conversion from <see cref="DirectoryPath"/> to string.</summary>
    public static implicit operator string(DirectoryPath path) => path.FullPath;

    /// <summary>Cast <paramref name="str"/> to <see cref="DirectoryPath"/> if not null or empty.</summary>
    public static implicit operator DirectoryPath(string str) => new DirectoryPath(str);

}
