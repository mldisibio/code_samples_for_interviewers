using System.Diagnostics.CodeAnalysis;
using contoso.functional.patterns.result;

namespace contoso.utility.compression.entities;

/// <summary>Wraps validation of an output directory path and ensures it exists.</summary>
public interface IOutputDirectory
{
    /// <summary>The validated full path of the output directory or an empty string if not valid.</summary>
    string FullPath { get; }
    /// <summary>Ensure directory is found on disk and create if necessary.</summary>
    Result<IOutputDirectory> PreVerify();
    /// <summary>Invoke before each directory access to ensure it is still found on disk even if initially validated.</summary>
    Result<IOutputDirectory> PostVerify();
    /// <summary>Count files in output directory. Returns failure if directory not found or empty.</summary>
    Result<int> GetFileCount();
    /// <summary>Remove the output directory.</summary>
    Result<IOutputDirectory> TryRemove();
}

/// <summary>Wraps validation of an output directory path and ensures it exists.</summary>
internal class OutputDirectory : IOutputDirectory
{
    Result<IOutputDirectory> _faulted = default;

    OutputDirectory(string resolvedDirectory, in Result<IOutputDirectory> faulted = default) => (FullPath, _faulted) = (resolvedDirectory, faulted);

    /// <summary>Factory method to validate <paramref name="outputDirectory"/> and ensure it exists.</summary>
    public static OutputDirectory CreateFrom(string? outputDirectory)
    {
        if (outputDirectory.IsNullOrEmptyString())
            return new OutputDirectory(string.Empty, Result<IOutputDirectory>.WithError(Error.OutputDirectoryPathIsEmpty));

        string resolvedDirectory;
        try
        {
            resolvedDirectory = Path.GetFullPath(outputDirectory);
        }
        catch (Exception ex)
        {
            return new OutputDirectory(outputDirectory, Result<IOutputDirectory>.WithError(Error.OutputDirectoryPathIsInvalid, ex));
        }

        try
        {
            Directory.CreateDirectory(resolvedDirectory);
            if (!Directory.Exists(resolvedDirectory))
                return new OutputDirectory(resolvedDirectory, Result<IOutputDirectory>.WithError(Error.OutputDirectoryCouldNotBeCreated));
        }
        catch (Exception ex)
        {
            return new OutputDirectory(resolvedDirectory, Result<IOutputDirectory>.WithError(Error.OutputDirectoryCouldNotBeCreated, ex));
        }

        return new OutputDirectory(resolvedDirectory);
    }

    /// <summary>The validated full path of the output directory or an empty string if not valid.</summary>
    public string FullPath { [return: NotNull]get; init; }

    /// <summary>Verify the output directory still exists and create if necessary.</summary>
    public Result<IOutputDirectory> PreVerify()
    {
        if (_faulted.Initialized)
            return _faulted;

        if (Directory.Exists(FullPath))
            return Result<IOutputDirectory>.WithSuccess(this);

        try { Directory.CreateDirectory(FullPath); }
        catch (Exception ex) { return Result<IOutputDirectory>.WithError(Error.OutputDirectoryCouldNotBeCreated, ex); }

        return Directory.Exists(FullPath)
               ? Result<IOutputDirectory>.WithSuccess(this)
               : Result<IOutputDirectory>.WithError(Error.OutputDirectoryCouldNotBeCreated);
    }

    /// <summary>Verify the output directory still exists.</summary>
    public Result<IOutputDirectory> PostVerify()
    {
        if (_faulted.Initialized)
            return _faulted;

        return Directory.Exists(FullPath)
               ? Result<IOutputDirectory>.WithSuccess(this)
               : Result<IOutputDirectory>.WithError(Error.OutputDirectoryNoLongerFound);
    }

    /// <summary>Count files in output directory. Returns failure if directory not found or empty.</summary>
    public Result<int> GetFileCount()
    {
        return PostVerify().OnSuccess(_ => CountFilesIn(FullPath));

        static Result<int> CountFilesIn(string directoryPath)
        {
            try
            {
                var outputInfo = new DirectoryInfo(directoryPath);
                int filesWithContent = outputInfo.EnumerateFiles(searchPattern: "*", searchOption: SearchOption.AllDirectories)
                                                 .Where(file => file.Length > 0)
                                                 .Count();
                return filesWithContent > 0
                       ? Result<int>.WithSuccess(filesWithContent)
                       : Result<int>.WithError(Error.OutputDirectoryIsEmpty);
            }
            catch (Exception countEx)
            {
                return Result<int>.WithError(Error.ExceptionWasThrown, countEx);
            }
        }
    }

    /// <summary>Remove the output directory.</summary>
    public Result<IOutputDirectory> TryRemove()
    {
        try
        {
            if (!string.IsNullOrEmpty(FullPath))
            {
                var info = new DirectoryInfo(FullPath);
                info.Delete(recursive: true);
            }
            return Result<IOutputDirectory>.WithSuccess(this);
        }
        catch (Exception ex)
        {
            return Result<IOutputDirectory>.WithError(Error.ExceptionWasThrown, ex);
        }
    }
}
