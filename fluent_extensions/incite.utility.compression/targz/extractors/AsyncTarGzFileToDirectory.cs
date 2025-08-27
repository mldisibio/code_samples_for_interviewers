using contoso.functional.patterns.result;
using contoso.utility.compression.entities;
using contoso.utility.compression.gzip;
using contoso.utility.compression.tar;
using contoso.utility.compression.targz.factories;

namespace contoso.utility.compression.targz.extractors;

internal sealed class AsyncTarGzFileToDirectory : UnzipAndExtractAsyncToDirectory
{
    readonly FileToDirectory _io;

    internal AsyncTarGzFileToDirectory(in FileToDirectory ioPair)
        : base()
    {
        _io = ioPair;

        _io.Verify()
           .AndEither(successAction: HandleCompletedRead,
                      failureAction: HandleFailedRead);

        // note: we don't explicitly succeed the OperationResult from the constructor, only explicitly fail
        FileToDirectory HandleCompletedRead(FileToDirectory io)
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
                    InputFile: _io.Input.FullPath,
                    OutputFile: null,
                    InputIsStream: false,
                    OutputIsStream: false,
                    Header: null,
                    Footer: null
                );
        }
    }

    /// <inheritdoc/>
    public override async Task<ExtractToDirectoryResult> UnzipAndExtractAsync()
    {
        if (!OperationResult.Ok)
            return OperationResult;

        try
        {
            // decompress the gzip to memory as tar
            MemoryStream unzippedStream;
            await using ((unzippedStream = new MemoryStream(TarGzReader.UnzipAllocation)).ConfigureAwait(false))
            {
                DecompressToStreamResult unzipResult = await GZipReader.CreateFor(_io.Input.FullPath)
                                                                       .AndExtractToStreamAsync(unzippedStream)
                                                                       .DecompressAsyncWithRetry()
                                                                       .ConfigureAwait(false);
                if (unzipResult.Success)
                {
                    // extract memory tar to output directory
                    unzippedStream.Seek(0, SeekOrigin.Begin);
                    return await TarReader.CreateFor(unzippedStream)
                                          .AndExtractToDirectoryAsync(_io.Output.FullPath)
                                          .ExtractAsync()
                                          .ConfigureAwait(false);
                }
                else
                {
                    OperationResult.SubLog.Error(Error.TarGzUnzipFailed);
                    OperationResult.Fail(unzipResult.Error);
                    return OperationResult;
                }
            }
        }
        catch (Exception ex)
        {
            OperationResult.Fail(exception: ex);
            return OperationResult;
        }
    }
}
