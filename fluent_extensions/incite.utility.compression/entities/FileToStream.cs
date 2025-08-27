using contoso.functional.patterns.result;

namespace contoso.utility.compression.entities;

/// <summary>Wraps an <see cref="IInputFile"/> and an <see cref="IOutputStream"/>pair.</summary>
internal readonly struct FileToStream
{
    public FileToStream(IInputFile input, IOutputStream output) => (Input, Output) = (input, output);

    public IInputFile Input { get; init; }

    public IOutputStream Output { get; init; }

    public Result<FileToStream> Verify()
    {
        var outputResult = Output.Verify();
        if (!outputResult.Success)
            return Result<FileToStream>.WithErrorFrom(outputResult);

        var inputResult = Input.Verify();
        return inputResult.Success
               ? Result<FileToStream>.WithSuccess(this)
               : Result<FileToStream>.WithErrorFrom(inputResult);
    }
}

