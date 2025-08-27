using contoso.functional.patterns.result;

namespace contoso.utility.compression.entities;

/// <summary>Wraps an <see cref="IInputFile"/> and <see cref="IOutputDirectory"/> pair.</summary>
internal readonly struct FileToDirectory
{
    public FileToDirectory(IInputFile input, IOutputDirectory output) => (Input, Output) = (input, output);

    public IInputFile Input { get; init; }

    public IOutputDirectory Output { get; init; }

    public Result<FileToDirectory> Verify()
    {
        var outputResult = Output.PreVerify();
        if (!outputResult.Success)
            return Result<FileToDirectory>.WithErrorFrom(outputResult);

        var inputResult = Input.Verify();
        return inputResult.Success
               ? Result<FileToDirectory>.WithSuccess(this)
               : Result<FileToDirectory>.WithErrorFrom(inputResult);
    }
}

