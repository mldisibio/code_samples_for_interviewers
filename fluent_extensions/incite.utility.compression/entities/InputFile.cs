using System.Diagnostics.CodeAnalysis;
using contoso.functional.patterns.result;

namespace contoso.utility.compression.entities;

/// <summary>Defines a wrapper that encapsulates input file path validation.</summary>
public interface IInputFile
{
    /// <summary>The validated full path or an empty string if not valid.</summary>
    string FullPath { get; }
    /// <summary>The validated directory of the file, or an empty string if not valid.</summary>
    string DirectoryPath { get; }
    /// <summary>Invoke before each file access to ensure it is still found on disk even if initially validated.</summary>
    Result<IInputFile> Verify();
    /// <summary>Returns the underlying file as a read-only <see cref="FileStream"/>.</summary>
    FileStream OpenRead();
    /// <summary>Returns the underlying file as a read-only <see cref="FileStream"/> opened for asynchronous operation.</summary>
    FileStream OpenReadForAsync();
    /// <summary>Copy the entire input file into a <see cref="MemoryStream"/>.</summary>
    DisposableResult<MemoryStream> CopyToMemory();
    /// <summary>Copy the input file into a <see cref="MemoryStream"/> trimming <paramref name="trimHeaderCount"/> bytes from start and <paramref name="trimFooterCount"/> bytes from end.</summary>
    DisposableResult<MemoryStream> TrimToMemory(int trimHeaderCount, int trimFooterCount);
    /// <summary>Asynchronously copy the entire input file into a <see cref="MemoryStream"/>.</summary>
    Task<DisposableResult<MemoryStream>> CopyToMemoryAsync();
    /// <summary>Asynchronously copy the input file into a <see cref="MemoryStream"/> trimming <paramref name="trimHeaderCount"/> bytes from start and <paramref name="trimFooterCount"/> bytes from end.</summary>
    Task<DisposableResult<MemoryStream>> TrimToMemoryAsync(int trimHeaderCount, int trimFooterCount);
}

/// <summary>Wraps generic initial input file validation and also allows operation to check input file has not been removed before accessing it.</summary>
internal class InputFile : IInputFile
{
    Result<IInputFile> _faulted = default;

    InputFile(string resolvedPath, string parentDirectory, Result<IInputFile> faulted = default) => (FullPath, DirectoryPath, _faulted) = (resolvedPath, parentDirectory, faulted);

    /// <summary>Factory method.</summary>
    public static InputFile CreateOver(string? inputFilePath)
    {
        if (inputFilePath.IsNullOrEmptyString())
            return new InputFile(string.Empty, string.Empty, Result<IInputFile>.WithError(Error.InputFilePathIsEmpty));

        string resolvedPath;
        try
        {
            resolvedPath = Path.GetFullPath(inputFilePath);
        }
        catch (Exception ex)
        {
            return new InputFile(inputFilePath, string.Empty, Result<IInputFile>.WithError(Error.InputFilePathIsInvalid, ex));
        }

        if (!File.Exists(resolvedPath))
            return new InputFile(resolvedPath, string.Empty, Result<IInputFile>.WithError(Error.InputFilePathNotFound));

        string? resolvedDirectory;
        try
        {
            resolvedDirectory = Path.GetDirectoryName(resolvedPath);
            if (resolvedDirectory.IsNullOrEmptyString())
                return new InputFile(resolvedPath, string.Empty, Result<IInputFile>.WithError(Error.InputDirectoryUndetermined));
        }
        catch (Exception ex)
        {
            return new InputFile(resolvedPath, string.Empty, Result<IInputFile>.WithError(Error.InputDirectoryUndetermined, ex));
        }

        return new InputFile(resolvedPath, resolvedDirectory);
    }

    /// <summary>The validated full path or an empty string if invalid.</summary>
    public string FullPath { [return: NotNull]get; init; }

    /// <summary>The parent directory of the input file, or an empty string if invalid.</summary>
    public string DirectoryPath { [return: NotNull]get; init; }

    /// <summary>Returns the underlying file as a read-only <see cref="FileStream"/>.</summary>
    public FileStream OpenRead() => new FileStream(FullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: false);

    /// <summary>Returns the underlying file as a read-only <see cref="FileStream"/> opened for asynchronous I/O.</summary>
    public FileStream OpenReadForAsync() => new FileStream(FullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);

    /// <summary>Invoke this before each file access to ensure it is still found on disk even if initially validated.</summary>
    public Result<IInputFile> Verify()
    {
        if (_faulted.Initialized)
            return _faulted;

        if (!File.Exists(FullPath))
        {
            _faulted = Result<IInputFile>.WithError(Error.InputFileNoLongerFound);
            return _faulted;
        }
        return Result<IInputFile>.WithSuccess(this);
    }

    /// <summary>Copy the entire input file into a <see cref="MemoryStream"/>.</summary>
    public DisposableResult<MemoryStream> CopyToMemory() => TrimToMemory(0, 0);

    /// <summary>Copy the input file into a <see cref="MemoryStream"/> trimming <paramref name="trimHeaderCount"/> bytes from start and <paramref name="trimFooterCount"/> bytes from end.</summary>
    public DisposableResult<MemoryStream> TrimToMemory(int trimHeaderCount, int trimFooterCount)
        => Verify().OnSuccess((inputFile) => Copy(inputFile, trimHeaderCount, trimFooterCount))
                   .AsDisposableResult();

    static Result<MemoryStream> Copy(IInputFile inputFile, int trimHeaderCount, int trimFooterCount)
    {
        try
        {
            using (FileStream input = inputFile.OpenRead())
            {
                long bytesToCopy = input.Length - trimHeaderCount - trimFooterCount;

                if (bytesToCopy > int.MaxValue)
                    return Result<MemoryStream>.WithError(Error.InputStreamLengthNotSupportedByMemoryStream);
                if (bytesToCopy <= 0)
                    return Result<MemoryStream>.WithError(Error.InputStreamTrimmedLessThanLength);

                input.Seek(trimHeaderCount, SeekOrigin.Begin);
                (bool Success, MemoryStream MemoryStream) result = input.TryCopyAsMemoryStream((int)bytesToCopy);
                if (result.Success)
                    return Result<MemoryStream>.WithSuccess(result.MemoryStream);
                else
                {
                    result.MemoryStream?.Dispose();
                    return Result<MemoryStream>.WithError(Error.ReturnedFalse);
                }
            }
        }
        catch (Exception copyEx)
        {
            return Result<MemoryStream>.WithError(Error.ExceptionWasThrown, copyEx);
        }
    }

    /// <summary>Asynchronously copy the entire input file into a <see cref="MemoryStream"/>.</summary>
    public Task<DisposableResult<MemoryStream>> CopyToMemoryAsync() => TrimToMemoryAsync(0, 0);

    /// <summary>Asynchronously copy the input file into a <see cref="MemoryStream"/> trimming <paramref name="trimHeaderCount"/> bytes from start and <paramref name="trimFooterCount"/> bytes from end.</summary>
    public async Task<DisposableResult<MemoryStream>> TrimToMemoryAsync(int trimHeaderCount, int trimFooterCount)
    {
        var memResult = await Verify().OnSuccessAsync((input) => CopyAsync(input, trimHeaderCount, trimFooterCount))
                                      .ConfigureAwait(false);
        return memResult.AsDisposableResult();
    }

    static async Task<Result<MemoryStream>> CopyAsync(IInputFile inputFile, int trimHeaderCount, int trimFooterCount)
    {
        try
        {
            FileStream input;
            await using ((input = inputFile.OpenReadForAsync()).ConfigureAwait(false))
            {
                long bytesToCopy = input.Length - trimHeaderCount - trimFooterCount;

                if (bytesToCopy > int.MaxValue)
                    return Result<MemoryStream>.WithError(Error.InputStreamLengthNotSupportedByMemoryStream);
                if (bytesToCopy <= 0)
                    return Result<MemoryStream>.WithError(Error.InputStreamTrimmedLessThanLength);

                input.Seek(trimHeaderCount, SeekOrigin.Begin);
                (bool Success, MemoryStream MemoryStream) result = await input.TryCopyAsMemoryStreamAsync((int)bytesToCopy);
                if (result.Success)
                    return Result<MemoryStream>.WithSuccess(result.MemoryStream);
                else
                {
                    result.MemoryStream?.Dispose();
                    return Result<MemoryStream>.WithError(Error.ReturnedFalse);
                }
            }
        }
        catch (Exception copyEx)
        {
            return Result<MemoryStream>.WithError(Error.ExceptionWasThrown, copyEx);
        }
    }

}
