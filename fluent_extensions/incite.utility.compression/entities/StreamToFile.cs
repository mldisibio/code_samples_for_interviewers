using contoso.functional.patterns.result;

namespace contoso.utility.compression.entities;

/// <summary>Wraps an <see cref="IInputStream"/> and <see cref="IOutputFile"/> pair.</summary>
internal readonly struct StreamToFile
{
    public StreamToFile(IInputStream input, IOutputFile output) => (Input, Output) = (input, output);

    internal StreamToFile(MemoryStream input, IOutputFile output) => (Input, Output) = (InputStream.CreateOver(input), output);

    public IInputStream Input { get; init; }

    public IOutputFile Output { get; init; }

    public Result<StreamToFile> Verify()
    {
        var outputResult = Output.Verify();
        if (!outputResult.Success)
            return Result<StreamToFile>.WithErrorFrom(outputResult);

        var inputResult = Input.Verify();
        return inputResult.Success
               ? Result<StreamToFile>.WithSuccess(this)
               : Result<StreamToFile>.WithErrorFrom(inputResult);
    }
}

