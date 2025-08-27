using contoso.functional.patterns.result;

namespace contoso.utility.compression.entities;

/// <summary>Wraps an <see cref="IInputStream"/> and <see cref="IOutputFile"/> pair.</summary>
internal readonly struct StreamToDirectory
{
    public StreamToDirectory(IInputStream input, IOutputDirectory output) => (Input, Output) = (input, output);

    /// <summary>For internal api to wrap input file path as <see cref="FileStream"/></summary>
    internal StreamToDirectory(FileStream input, IOutputDirectory output) => (Input, Output) = (InputStream.CreateOver(input), output);

    internal StreamToDirectory(MemoryStream input, IOutputDirectory output) => (Input, Output) = (InputStream.CreateOver(input), output);

    public IInputStream Input { get; init; }

    public IOutputDirectory Output { get; init; }

    public Result<StreamToDirectory> Verify()
    {
        var outputResult = Output.PreVerify();
        if (!outputResult.Success)
            return Result<StreamToDirectory>.WithErrorFrom(outputResult);

        var inputResult = Input.Verify();
        return inputResult.Success
               ? Result<StreamToDirectory>.WithSuccess(this)
               : Result<StreamToDirectory>.WithErrorFrom(inputResult);
    }
}
