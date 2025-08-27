using contoso.functional.patterns.result;
using contoso.utility.compression.entities;
using contoso.utility.compression.tar.entities;

namespace contoso.utility.compression.tar;

/// <summary>Asynchronously extract a tar archive to a directory.</summary>
public sealed class TarAsyncReader
{
    /// <summary>Actual extraction code for a tar archive stream.</summary>
    internal static Task<Result<StreamToDirectory>> ExtractCoreAsync(TarReaderInfo tarReaderInfo)
        => tarReaderInfo.Verify().OnSuccessAsync(ParseHeadersAsync);

    /// <summary>Parse each tar header and extract all archived files and directories to the output directory on disk.</summary>
    /// <remarks>Attempts basic corruption compensation, given issues specific to Oasis/VLEx.</remarks>
    static async Task<Result<StreamToDirectory>> ParseHeadersAsync(TarReaderInfo readerInfo)
    {
        byte[] headerBuffer = new byte[TarHeaderBlock.BlockLength];
        try
        {
            // read the next block
            // no use proceeding unless we have read a full block;
            // but a full block does not yet mean the header is valid or not corrupted
            while (await headerBuffer.TryFillAsyncFrom(readerInfo.InputStream).ConfigureAwait(false))
            {
                // we've read a full block, valid or not, so create a header instance
                var header = new TarHeaderBlock(headerBuffer);

                // debug only
                //readerInfo.OpResult.DebugHeader(header);

                // update the TarReaderInfo with the header
                readerInfo = readerInfo.With(header);

                // a valid header is 512 bytes and has 'ustar' in the right position
                if (header.ValidHeader)
                {
                    try
                    {
                        await ComposeExtractIO(readerInfo).OnSuccessAsync(ExtractArchiveFile)
                                                          .OnSuccessAsync(ValidateExtractedFile)
                                                          .OnSuccessAsync(MoveStreamToEndOfBlock)
                                                          .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        readerInfo.OpResult.SubLog.Error(Error.TarExtractionFailedForCurrentHeader, ex);
                    }
                }
                else
                {
                    TryRealignStreamWithHeader(readerInfo);
                }
                // at this point position is at either the next header or at an empty block of binary zeros
            } // end read loop
        }
        catch (Exception ex)
        {
            readerInfo.OpResult.SubLog.Error(Error.ExceptionWasThrown, ex);
        }
        // if we've extracted any files at all, we will return success
        return readerInfo.OpResult.ExtractedFiles.Count > 0
               ? Result<StreamToDirectory>.WithSuccess(readerInfo.IO)
               : Result<StreamToDirectory>.WithError(Error.TarExtractionFailedForEntireArchive);
    }

    /// <summary>Compose the i/o configuration for the individual archive file in context.</summary>
    static Result<TarReaderInfo> ComposeExtractIO(TarReaderInfo readerInfo)
    {
        if (readerInfo.HeaderBlock.HasFile)
        {
            readerInfo.OpResult.IncrementExpectedFileCount();
            try
            {
                // compose the full output path for the tar file entry;
                string fullOutputPath = Path.GetFullPath(Path.Join(readerInfo.OutputDirectory.FullPath, readerInfo.HeaderBlock.Name));
                // compose a new IOutputFile for this individual file extract; OutputFile will ensure directory path exists;
                // update the TarReaderInfo with the IOutputFile
                return OutputFile.CreateOver(fullOutputPath)
                                 .Verify()
                                 .MapResultValueTo(outputFile => readerInfo.With(outputFile));
            }
            catch (Exception ex)
            {
                readerInfo.OpResult.SubLog.Error(exception: ex);
                return Result<TarReaderInfo>.WithError(Error.ExceptionWasThrown, ex);
            }
        }
        // if the tar entry is a directory, it's not really an 'error', but we want to skip to the next header 
        return Result<TarReaderInfo>.WithError(Error.OutputFilePathIsEmpty);
    }

    /// <summary>Extracts an individual archive file entry to a file on disk</summary>
    static async Task<Result<TarReaderInfo>> ExtractArchiveFile(TarReaderInfo readerInfo)
    {
        IOutputFile outputFile = readerInfo.ExtractedFile;
        readerInfo.OpResult.SubLog.Debug(message: $"Extracting {readerInfo.ExpectedFileSize:N0} bytes to {outputFile.FullPath}");

        byte[] fileBuffer = new byte[TarHeaderBlock.BlockLength];
        long totalBytesRead = 0;

        try
        {
            // note this will also create any zero-length files
            FileStream outputStream;
            await using ((outputStream = outputFile.Create(useAsync: true)).ConfigureAwait(false))
            {
                long bytesRemaining = readerInfo.ExpectedFileSize;
                int bytesRead = 0;

                do
                {
                    // read the archived file bytes in chunks of blocks
                    int countToRead = (int)Math.Min(TarHeaderBlock.BlockLength, bytesRemaining);
                    totalBytesRead += (bytesRead = await readerInfo.InputStream.ReadAsyncWithCountInto(fileBuffer, countToRead).ConfigureAwait(false));
                    bytesRemaining -= bytesRead;
                    if (bytesRead > 0)
                        await outputStream.WriteAsync(fileBuffer.AsMemory()[..bytesRead]).ConfigureAwait(false);
                    // zero bytesRead before zero bytesRemaining means we unexpectedly reached end of outer tar file;
                    // for partially unzipped tgz files, this might occur if the archived file bytes are truncated
                    // before the expected file size originally written to the header of the last file is actually reached;
                    // for caf2, this might represent a few missing lines, yet still yield thousands of salvaged records
                }
                while (bytesRemaining > 0 && bytesRead > 0);

                outputStream.Flush();
            }
        }
        catch (Exception extractEx)
        {
            readerInfo.OpResult.SubLog.Error(exception: extractEx);
            return Result<TarReaderInfo>.WithError(Error.TarExtractionFailedForCurrentHeader, extractEx);
        }
        // update the TarReaderInfo with the bytes consumed from the end of the current header block in context
        return Result<TarReaderInfo>.WithSuccess(readerInfo.With(totalBytesRead));
    }

    static Result<TarReaderInfo> ValidateExtractedFile(TarReaderInfo readerInfo)
    {
        return readerInfo.ExtractedFile
                         .VerifyLength()
                         .MapResultValueTo(_ => readerInfo)
                         .AndEither(successAction: OnExtractionSuccess,
                                    failureAction: err => TryCleanupFailedExtract(err, readerInfo));

        static TarReaderInfo OnExtractionSuccess(TarReaderInfo readerInfo)
        {
            readerInfo.OpResult.AddExtractedFile(readerInfo.ExtractedFile.FullPath);
            return readerInfo;
        }

        // attempt to remove any empty or partially created output file
        static void TryCleanupFailedExtract(IErrorResult error, TarReaderInfo info)
        {
            // log the case where an output was created but is zero length
            if (error.Error == Error.OutputFileIsEmpty)
                info.OpResult.SubLog.Error("Extract is zero-length and will be removed");
            // try to cleanup whether or not the reason was a zero-length output
            var cleanupResult = info.ExtractedFile.TryRemove();
            if (!cleanupResult.Success)
                info.OpResult.SubLog.Error(cleanupResult.ErrorMessage);
        }
    }

    /// <summary>When done extracting the file, move the stream position to the end of the current block.</summary>
    static Result<TarReaderInfo> MoveStreamToEndOfBlock(TarReaderInfo readerInfo)
    {
        // NOTE: if seeking next block boundary fails, then we let the exception bubble because this should stop processing for the entire archive file
        int bytesToConsume = BytesTillEndOfBlock(readerInfo.ConsumedCount, TarHeaderBlock.BlockLength);
        // move to end of block or end of stream, whichever comes first
        bytesToConsume = (int)Math.Min(bytesToConsume, readerInfo.InputLengthAvailable);
        if (bytesToConsume > 0)
            readerInfo.InputStream.Seek(bytesToConsume, SeekOrigin.Current);
        return Result<TarReaderInfo>.WithSuccess(readerInfo);
    }

    /// <summary>
    /// Given the current position in a tar archive, calculate the remaining number of bytes
    /// to reach the end of the current <paramref name="blockSize"/> (512) block.
    /// </summary>
    /// <remarks>
    /// It is expected that when file data does not fill out the full block of 512 bytes,
    /// the remainder is filled with zeros and the stream position must be advanced to the beginning of the next block.
    /// </remarks>
    static int BytesTillEndOfBlock(long bytesRead, int blockSize)
    {
        // ------------------------------
        // given the need to compensate for file corruption by occassionally 're-aligning' the header,
        // this function cannot be as simple as the calculating the next 512 end from the current stream position!
        // or, put differently, 'totalBytesRead' is no longer the same as 'Stream.Position' once there is a need to 're-align'
        // ------------------------------
        long needToMove = blockSize - (bytesRead % blockSize);
        if (needToMove > blockSize)
            throw new IndexOutOfRangeException("Overflow calculating 'TillEndOfBlock'");
        return needToMove == blockSize ? 0 : (int)needToMove;
    }
    
    /// <summary>Attempt to re-align the stream position such that 'ustar' is found in the correct position.</summary>
    static Result<TarReaderInfo> TryRealignStreamWithHeader(TarReaderInfo readerInfo)
    {
        if (readerInfo.HeaderBlock.JunkOffset.HasValue)
        {
            int skipCount = readerInfo.HeaderBlock.JunkOffset.Value;
            // ------------------------------
            // corruption compensation #1
            // if the magic string 'ustar' is anywhere in our buffer, we can adjust the alignment window
            // ------------------------------
            try
            {
                // since the header is invalid, go back to start of block
                readerInfo.InputStream.Seek(TarHeaderBlock.BlockLength * -1, SeekOrigin.Current);
                // and readjust to align magic string where it should be
                readerInfo.InputStream.Seek(skipCount, SeekOrigin.Current);
                if (skipCount > 0)
                    readerInfo.OpResult.SubLog.Debug(message: $"Skipping: {skipCount} bytes of junk");
                else
                    readerInfo.OpResult.SubLog.Debug(message: $"Rewinding: {skipCount} bytes");
                // the offset is usually perpetuated into each subsequent header, so our current position now becomes the 'start of 512 block'
                // no matter what our absolute stream position actually is
            }
            catch (Exception ex)
            {
                readerInfo.OpResult.SubLog.Error(exception: ex);
                // if re-alignment fails, it's not critical enough to stop processing the archive;
                // we'll just continue consuming blocks looking for the next valid header
                return Result<TarReaderInfo>.WithError(Error.TarRealignmentFailedForCurrentHeader, ex);
            }
        }
        return Result<TarReaderInfo>.WithSuccess(readerInfo);
    }
}
