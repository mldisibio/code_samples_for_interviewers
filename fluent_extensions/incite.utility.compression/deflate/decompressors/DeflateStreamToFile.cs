using contoso.functional.patterns.result;
using contoso.utility.compression.entities;

namespace contoso.utility.compression.deflate.decompressors;

/// <summary>Utility to decompress the configured source stream to the configured output file.</summary>
/// <example>
///     <code>
///        DeflateStreamToFile deflateReader = DeflateReader.CreateFor(deflateStream).AndExtractToFile('path_to_output_file');
///     </code>
/// </example>
internal sealed class DeflateStreamToFile : InflateToFile
{
    readonly StreamToFile _io;

    internal DeflateStreamToFile(in StreamToFile ioPair)
        : base()
    {
        _io = ioPair;

        _io.Verify()
           .AndEither(successAction: HandleCompletedRead,
                      failureAction: HandleFailedRead);

        // note: we don't explicitly succeed the OperationResult from the constructor, only explicitly fail
        StreamToFile HandleCompletedRead(StreamToFile io)
        {
            CaptureState();
            return io;
        }

        void HandleFailedRead(IErrorResult error)
        {
            CaptureState();
            // fail operation with first error
            OperationResult.Fail(error.ErrorMessage);
        }

        void CaptureState()
        {
            // capture input state for debugging
            OperationResult.InputState = new
                (
                    InputFile: null,
                    OutputFile: _io.Output.FullPath,
                    InputIsStream: true,
                    OutputIsStream: false,
                    Header: null,
                    Footer: null
                );
        }
    }

    /// <inheritdoc/>
    public override DecompressToFileResult Decompress()
    {
        if (!OperationResult.Ok)
            return OperationResult;

        DecompressInputStream(_io)
        .AndEither(successAction: SetOperationSuccess,
                   failureAction: SetOperationFailure);

        return OperationResult;
    }

    Result<IOutputFile> DecompressInputStream(StreamToFile io)
    {
        OperationResult.SubLog.Debug(DeflateReader.DecompressStartMsg);

        return io.Verify()
                 .OnSuccess(InvokeDecompressCore)
                 .OnSuccess(VerifyOutputFile);

        static Result<IOutputFile> InvokeDecompressCore(StreamToFile io)
        {
            try
            {
                // create the output file stream
                using FileStream outputStream = io.Output.Create();
                // wrap the streams
                var ioPair = new StreamToStream(io.Input, outputStream);
                // decompress the input stream to the output stream and return the output file
                return DeflateReader.DecompressCore(ioPair)
                                    .MapResultValueTo(_ => io.Output);
            }
            catch (Exception ex)
            {
                return Result<IOutputFile>.WithError(Error.ExceptionWasThrown, ex);
            }
        }
    }
}

