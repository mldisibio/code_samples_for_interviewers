using System.Diagnostics.CodeAnalysis;
using contoso.functional.patterns.result;

namespace contoso.utility.compression.entities;

/// <summary>Defines a wrapper that encapsulates output file path validation.</summary>
public interface IOutputFile
{
    /// <summary>The validated full path or an empty string if not valid.</summary>
    string FullPath { get; }
    /// <summary>The validated directory of the file, or an empty string if not valid.</summary>
    string DirectoryPath { get; }
    /// <summary>Invoke before each file access to ensure it is still found on disk even if initially validated.</summary>
    Result<IOutputFile> Verify();
    /// <summary>Invoke to ensure the file exists and has content.</summary>
    Result<long> VerifyLength();
    /// <summary>Remove the output file.</summary>
    Result<IOutputFile> TryRemove();
    /// <summary>Creates or overwrites the underlying file as a read-write <see cref="FileStream"/>.</summary>
    FileStream Create(bool useAsync = false);
}

/// <summary>Wraps generic one-time validation of an output file path. Also ensures the output directory exists.</summary>
internal class OutputFile : IOutputFile
{
    Result<IOutputFile> _faulted = default;

    OutputFile(string resolvedPath, string resolvedDirectory, in Result<IOutputFile> faulted = default) => (FullPath, DirectoryPath, _faulted) = (resolvedPath, resolvedDirectory, faulted);

    /// <summary>Factory method when full path to output file is given. This simply validates the path and ensures its directory exists.</summary>
    public static OutputFile CreateOver(string? outputFilePath)
    {
        if (outputFilePath.IsNullOrEmptyString())
            return new OutputFile(string.Empty, string.Empty, Result<IOutputFile>.WithError(Error.OutputFilePathIsEmpty));

        string resolvedPath;
        try
        {
            resolvedPath = Path.GetFullPath(outputFilePath);
        }
        catch (Exception ex)
        {
            return new OutputFile(outputFilePath, string.Empty, Result<IOutputFile>.WithError(Error.OutputFilePathIsInvalid, ex));
        }

        string? resolvedDirectory;
        try
        {
            if (!FileSystemHelp.EnsureDirectoryExistsFor(resolvedPath, out resolvedDirectory))
                return new OutputFile(resolvedPath, string.Empty, Result<IOutputFile>.WithError(Error.OutputDirectoryCouldNotBeCreated));
        }
        catch (Exception ex)
        {
            return new OutputFile(resolvedPath, string.Empty, Result<IOutputFile>.WithError(Error.OutputDirectoryCouldNotBeCreated, ex));
        }

        return new OutputFile(resolvedPath, resolvedDirectory);
    }

    /// <summary>
    /// Factory method when only full path to output directory is given and full output path will have same file name as input file.
    /// This simply validates the composed output path and ensures its directory exists.
    /// </summary>
    public static OutputFile CreateFrom(string? outputDirectory, IInputFile inputValidator, bool removeExtension)
    {
        Result<IInputFile> inputValidation = inputValidator.Verify();
        if (!inputValidation.Success)
            return new OutputFile(string.Empty, outputDirectory ?? string.Empty, Result<IOutputFile>.WithErrorFrom(inputValidation));

        if (outputDirectory.IsNullOrEmptyString())
            return new OutputFile(string.Empty, string.Empty, Result<IOutputFile>.WithError(Error.OutputDirectoryPathIsEmpty));

        string resolvedDirectory;
        try
        {
            resolvedDirectory = Path.GetFullPath(outputDirectory);
        }
        catch (Exception ex)
        {
            return new OutputFile(string.Empty, outputDirectory, Result<IOutputFile>.WithError(Error.OutputDirectoryPathIsInvalid, ex));
        }

        string resolvedPath;
        try
        {
            string inputFileName = removeExtension
                                 ? Path.GetFileNameWithoutExtension(inputValidator.FullPath)
                                 : Path.GetFileName(inputValidator.FullPath);
            resolvedPath = Path.GetFullPath(Path.Join(resolvedDirectory, inputFileName));
        }
        catch (Exception ex)
        {
            return new OutputFile(string.Empty, resolvedDirectory, Result<IOutputFile>.WithError(Error.OutputFilePathIsInvalid, ex));
        }

        // verify the output path created by composing output directory with input name, and ensure the output directory exists
        return CreateOver(resolvedPath);
    }

    /// <summary>The validated full path or an empty string.</summary>
    public string FullPath { [return: NotNull]get; init; }

    /// <summary>The validated directory of the output file, or an empty string.</summary>
    public string DirectoryPath { [return: NotNull]get; init; }

    /// <summary>Creates or overwrites the underlying file as a read-write <see cref="FileStream"/>.</summary>
    public FileStream Create(bool useAsync = false) => new FileStream(FullPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, useAsync);

    /// <summary>Output file path itself need only be validated once.</summary>
    public Result<IOutputFile> Verify() => _faulted.Initialized ? _faulted : Result<IOutputFile>.WithSuccess(this);

    /// <summary>Ensure the input file still exists and has content.</summary>
    public Result<long> VerifyLength() => Verify().OnSuccess(_ => GetLength(FullPath));

    static Result<long> GetLength(string fullPath)
    {
        try
        {
            var info = new FileInfo(fullPath);
            if (!info.Exists)
                return Result<long>.WithError(Error.OutputFilePathNoLongerFound);
            long length = new FileInfo(fullPath).Length;
            return length > 0 ? Result<long>.WithSuccess(length) : Result<long>.WithError(Error.OutputFileIsEmpty);
        }
        catch (Exception ex)
        {
            return Result<long>.WithError(Error.ExceptionWasThrown, ex);
        }
    }

    /// <summary>Remove the output file.</summary>
    public Result<IOutputFile> TryRemove()
    {
        try
        {
            if (!string.IsNullOrEmpty(FullPath))
            {
                var info = new FileInfo(FullPath);
                info.Delete();
            }
            return Result<IOutputFile>.WithSuccess(this);
        }
        catch (Exception ex)
        {
            return Result<IOutputFile>.WithError(Error.ExceptionWasThrown, ex);
        }
    }
}



