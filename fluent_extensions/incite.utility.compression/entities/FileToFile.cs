using contoso.functional.patterns.result;

namespace contoso.utility.compression.entities;

/// <summary>Wraps an <see cref="IInputFile"/> and <see cref="IOutputFile"/> pair.</summary>
internal readonly struct FileToFile
{
    public FileToFile(IInputFile input, IOutputFile output) => (Input, Output) = (input, output);

    public IInputFile Input { get; init; }

    public IOutputFile Output { get; init; }

    public Result<FileToFile> Verify()
    {
        var outputResult = Output.Verify();
        if (!outputResult.Success)
            return Result<FileToFile>.WithErrorFrom(outputResult);

        var inputResult = Input.Verify();
        return inputResult.Success
               ? Result<FileToFile>.WithSuccess(this)
               : Result<FileToFile>.WithErrorFrom(inputResult);
    }
}

