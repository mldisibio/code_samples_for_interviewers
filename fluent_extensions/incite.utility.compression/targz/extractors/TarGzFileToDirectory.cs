using contoso.functional.patterns.result;
using contoso.utility.compression.entities;
using contoso.utility.compression.gzip;
using contoso.utility.compression.tar;
using contoso.utility.compression.targz.factories;

namespace contoso.utility.compression.targz.extractors;

internal sealed class TarGzFileToDirectory : UnzipAndExtractToDirectory
{
    readonly FileToDirectory _io;

    internal TarGzFileToDirectory(in FileToDirectory ioPair)
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
    public override ExtractToDirectoryResult UnzipAndExtract()
    {
        if (!OperationResult.Ok)
            return OperationResult;

        try
        {
            // decompress the gzip to memory as tar
            using (MemoryStream unzippedStream = new MemoryStream(TarGzReader.UnzipAllocation))
            {
                DecompressToStreamResult unzipResult = GZipReader.CreateFor(_io.Input.FullPath)
                                                                 .AndExtractToStream(unzippedStream)
                                                                 .DecompressWithRetry();
                if (unzipResult.Success)
                {
                    // extract memory tar to output directory
                    unzippedStream.Seek(0, SeekOrigin.Begin);
                    return TarReader.CreateFor(unzippedStream)
                                    .AndExtractToDirectory(_io.Output.FullPath)
                                    .Extract();
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
