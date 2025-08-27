using contoso.functional.patterns.result;

namespace contoso.utility.compression;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
/// <summary>Decompression operation errors.</summary>
public static class Error
{
    public static ErrorValue ReturnedFalse { get; } = ErrorValue.Of("Returned False");
    public static ErrorValue ExceptionWasThrown { get; } = ErrorValue.Of("Exception Was Thrown");

    public static ErrorValue InputFilePathIsEmpty { get; } = ErrorValue.Of("Input File Path Is Empty");
    public static ErrorValue InputFilePathIsInvalid { get; } = ErrorValue.Of("Input File Path Is Invalid");
    public static ErrorValue InputFilePathCannotBeOpenedForRead { get; } = ErrorValue.Of("Input File Path Cannot Be Opened For Read");
    public static ErrorValue InputFilePathNotFound { get; } = ErrorValue.Of("Input File Path Not Found");
    public static ErrorValue InputDirectoryUndetermined { get; } = ErrorValue.Of("Input Directory Undetermined");
    public static ErrorValue InputFileNoLongerFound { get; } = ErrorValue.Of("Input File No Longer Found");
    public static ErrorValue InputFileIsEmpty { get; } = ErrorValue.Of("Input File Is Empty");

    public static ErrorValue InputStreamIsNull { get; } = ErrorValue.Of("Input Stream Is Null");
    public static ErrorValue InputStreamDoesNotSupportRead { get; } = ErrorValue.Of("Input Stream Does Not Support Read");
    public static ErrorValue InputStreamDoesNotSupportSeek { get; } = ErrorValue.Of("Input Stream Does Not Support Seek");
    public static ErrorValue InputStreamIsEmpty { get; } = ErrorValue.Of("Input Stream Is Empty");
    public static ErrorValue InputStreamTrimmedLessThanLength { get; } = ErrorValue.Of("Input Stream Trimmed Less Than Length");
    public static ErrorValue InputStreamLengthLessThanMinimumGZip { get; } = ErrorValue.Of("Input Stream Length Less Than Minimum GZip");
    public static ErrorValue InputStreamLengthLessThanMinimumZLib { get; } = ErrorValue.Of("Input Stream Length Less Than Minimum ZLib");
    public static ErrorValue InputStreamLengthNotSupportedByMemoryStream { get; } = ErrorValue.Of("Input Stream Length Greater Than Int.MaxValue Capacity of MemoryStream");
    public static ErrorValue InputStreamIsDisposed { get; } = ErrorValue.Of("Input Stream Is Disposed");

    public static ErrorValue CannotReadHeaderLengthFromStream { get; } = ErrorValue.Of("Cannot Read Header Length From Stream");
    public static ErrorValue CannotReadFooterLengthFromStream { get; } = ErrorValue.Of("Cannot Read Footer Length From Stream");
    public static ErrorValue CannotReadValidGZipSignatureFromStream { get; } = ErrorValue.Of("Cannot Read Valid GZip Signature From Stream");
    public static ErrorValue CannotReadValidZLibSignatureFromStream { get; } = ErrorValue.Of("Cannot Read Valid ZLib Signature From Stream");

    public static ErrorValue OutputDirectoryPathIsEmpty { get; } = ErrorValue.Of("Output Directory Path Is Empty");
    public static ErrorValue OutputDirectoryPathIsInvalid { get; } = ErrorValue.Of("Output Directory Path Is Invalid");
    public static ErrorValue OutputDirectoryCouldNotBeCreated { get; } = ErrorValue.Of("Output Directory Could Not Be Created");
    public static ErrorValue OutputDirectoryNoLongerFound { get; } = ErrorValue.Of("Output Directory No Longer Found");
    public static ErrorValue OutputDirectoryIsEmpty { get; } = ErrorValue.Of("Output Directory Is Empty");

    public static ErrorValue OutputFilePathIsEmpty { get; } = ErrorValue.Of("Output File Path Is Empty");
    public static ErrorValue OutputFilePathIsInvalid { get; } = ErrorValue.Of("Output File Path Is Invalid");
    public static ErrorValue OutputFilePathNoLongerFound { get; } = ErrorValue.Of("Output File Path No Longer Found");
    public static ErrorValue OutputFilePathCannotBeOpenedForWrite { get; } = ErrorValue.Of("Output File Path Cannot Be Opened For Write");
    public static ErrorValue OutputFileIsEmpty { get; } = ErrorValue.Of("Output File Is Empty");

    public static ErrorValue OutputStreamIsNull { get; } = ErrorValue.Of("Output Stream Is Null");
    public static ErrorValue OutputStreamDoesNotSupportWrite { get; } = ErrorValue.Of("Output Stream Does Not Support Write");
    public static ErrorValue OutputStreamIsEmpty { get; } = ErrorValue.Of("Output Stream Is Empty");
    public static ErrorValue OutputStreamIsDisposed { get; } = ErrorValue.Of("Output Stream Is Disposed");

    public static ErrorValue DeflateStreamCouldNotRereadUpToError { get; } = ErrorValue.Of("Could Not Consume Deflate Decompression Stream Up To Error");
    public static ErrorValue DeflateStreamNothingWasRead { get; } = ErrorValue.Of("Could Not Read Any Bytes From Deflate Decompression Stream");
    public static ErrorValue DeflateStreamOperationFailed { get; } = ErrorValue.Of("Raw Deflate Decompression Operation Failed");

    public static ErrorValue TarExtractionFailedForCurrentHeader { get; } = ErrorValue.Of("Tar Extraction Failed For Current Header. Skipping To Next");
    public static ErrorValue TarRealignmentFailedForCurrentHeader { get; } = ErrorValue.Of("Tar Header Realignment Failed. Skipping To Next Header");
    public static ErrorValue TarExtractionFailedForEntireArchive { get; } = ErrorValue.Of("Tar Extraction Failed For Entire Archive. No Files Could Be Extracted");
    public static ErrorValue TarGzUnzipFailed { get; } = ErrorValue.Of("Unzipping of TarGz Archive Failed");

}
#pragma warning restore CS1591
