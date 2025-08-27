
using System.Diagnostics.CodeAnalysis;

namespace contoso.utility.iohelp;

/// <summary>Wraps a string as a strongly typed file path.</summary>
public struct FilePath : IEquatable<FilePath>
{
    readonly int? _hash;

    /// <summary>Initialize with a string representing a file path.</summary>
    /// <param name="path">A string representing a file path.</param>
    /// <param name="throwIfInvalid">True to throw any invalid path exception immediately (default). False to not throw exception and set <see cref="IsValid"/> to false.</param>
    public FilePath(string? path, bool throwIfInvalid = true)
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
                FileInfo info = new FileInfo(Path.GetFullPath(path));
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

    /// <summary>True if submitted string was parsed as a file path without error. Does not mean file exists.</summary>
    public bool IsValid { get; private set; }

    /// <summary>Any exception message when parsing the string submitted as a path, or null if no exception was encountered.</summary>
    public string? Error { get; private set; }

    /// <summary>Return the path as a <see cref="FileInfo"/> instance. Any exception is thrown.</summary>
    public FileInfo AsFileInfo() => new FileInfo(FullPath);

    /// <summary>Wraps check for existence in a try catch.</summary>
    public bool Exists()
    {
        try { return TryGetFileInfo(out FileInfo? info) && info.Exists; }
        catch { return false; }
    }

    /// <summary>Returns true if the path is valid and a <see cref="FileInfo"/> instance is created from it.</summary>
    public bool TryGetFileInfo([NotNullWhen(true)] out FileInfo? fileInfo)
    {
        if (!IsValid)
        {
            fileInfo = null;
            return false;
        }
        try
        {
            fileInfo = new FileInfo(FullPath);
            return true;
        }
        catch(Exception ex)
        {
            fileInfo = null;
            IsValid = false;
            Error = ex.Message;
            return false;
        }
    }

    /// <summary>Ensure the full directory path to the path represented by the current instance is created.</summary>
    public bool TryEnsureDirectoryExists([NotNullWhen(true)] out string? directoryPath)
    {
        if (!IsValid)
        {
            directoryPath = null;
            return false;
        }
        try
        {
            return FileSystemHelp.EnsureDirectoryExistsFor(FullPath, out directoryPath);
        }
        catch (Exception ex)
        {
            directoryPath = null;
            IsValid = false;
            Error = ex.Message;
            return false;
        }
    }

    /// <inheritdoc/>
    public override string ToString() => FullPath ?? string.Empty;

    /// <inheritdoc/>
    public override int GetHashCode() => _hash.GetValueOrDefault();

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj) => (obj is FilePath other) && Equals(other);

    /// <summary>True if both fully qualified absolute paths valid and exactly equal.</summary>
    public bool Equals(FilePath other) => string.Equals(FullPath, other.FullPath, StringComparison.InvariantCulture) && string.Equals(Error, other.Error, StringComparison.InvariantCulture);

    /// <summary>True if both fully qualified absolute paths valid and equal, case-insensitive.</summary>
    public bool EqualsIgnoringCase(FilePath other) => string.Equals(FullPath, other.FullPath, StringComparison.InvariantCultureIgnoreCase) && string.Equals(Error, other.Error, StringComparison.InvariantCultureIgnoreCase);

    /// <summary>True if both fully qualified absolute paths valid and exactly equal.</summary>
    public static bool operator ==(FilePath left, FilePath right) => left.Equals(right);

    /// <summary>True if either path is invalid or both paths are not exactly equal.</summary>
    public static bool operator !=(FilePath left, FilePath right) => !(left == right);

    /// <summary>Implicit conversion from <see cref="FilePath"/> to string.</summary>
    public static implicit operator string(FilePath path) => path.FullPath;

    /// <summary>Cast <paramref name="str"/> to <see cref="FilePath"/> if not null or empty.</summary>
    public static implicit operator FilePath(string str) => new FilePath(str);
}
