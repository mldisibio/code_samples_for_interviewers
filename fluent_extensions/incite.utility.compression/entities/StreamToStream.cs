using contoso.functional.patterns.result;

namespace contoso.utility.compression.entities;

/// <summary>Wraps a pair of opened input and output streams.</summary>
internal readonly struct StreamToStream
{
    /// <summary>Public api should validate the provided streams first.</summary>
    public StreamToStream(IInputStream input, IOutputStream output) => (Input, Output) = (input, output);

    /// <summary>For internal api to wrap one or both file paths as <see cref="FileStream"/></summary>
    internal StreamToStream(FileStream input, FileStream output) => (Input, Output) = (InputStream.CreateOver(input), OutputStream.CreateOver(output));

    /// <summary>For internal api to wrap one or both file paths as <see cref="FileStream"/></summary>
    internal StreamToStream(FileStream input, IOutputStream output) => (Input, Output) = (InputStream.CreateOver(input), output);

    /// <summary>For internal api to wrap one or both file paths as <see cref="FileStream"/></summary>
    internal StreamToStream(IInputStream input, FileStream output) => (Input, Output) = (input, OutputStream.CreateOver(output));

    /// <summary>For internal api to wrap the <see cref="MemoryStream"/> copy of the input stream</summary>
    internal StreamToStream(MemoryStream input, IOutputStream output) => (Input, Output) = (InputStream.CreateOver(input), output);

    public IInputStream Input { get; init; }

    public IOutputStream Output { get; init; }

    public Result<StreamToStream> Verify()
    {
        var outputResult = Output.Verify();
        if (!outputResult.Success)
            return Result<StreamToStream>.WithErrorFrom(outputResult);

        var inputResult = Input.Verify();
        return inputResult.Success
               ? Result<StreamToStream>.WithSuccess(this)
               : Result<StreamToStream>.WithErrorFrom(inputResult);
    }
}

