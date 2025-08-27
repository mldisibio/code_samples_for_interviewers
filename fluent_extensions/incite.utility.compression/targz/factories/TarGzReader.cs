using contoso.utility.compression.entities;
using contoso.utility.compression.targz.factories;

namespace contoso.utility.compression.targz;

/// <summary>Unzip and extract a tar archive to a directory.</summary>
public sealed class TarGzReader
{
    internal const string ExtractionStartMsg = "START TarGz Archive Extraction";
    internal const int UnzipAllocation = 0x400000; // 4Mb

    /// <summary>Configure a reader factory for the given <paramref name="inputFileName"/>.</summary>
    /// <param name="inputFileName">Full path to a zipped tar archive file.</param>
    /// <example><code>var reader = TarGzReader.CreateFor('path_to_input_file').AndExtractToDirectory('path_to_output_directory');</code></example>
    public static ITarGzFileConfig CreateFor(string inputFileName)
    {
        IInputFile inputFile = InputFile.CreateOver(inputFileName);
        return new TarGzInputFileConfig(inputFile);
    }

    /// <summary>Configure a reader factory for the given zipped <paramref name="tarArchiveStream"/>.</summary>
    /// <param name="tarArchiveStream">An open stream containing a zipped tar archive.</param>
    /// <example><code>var reader = TarReader.CreateFor(inputStream).AndExtractToDirectory('path_to_output_directory');</code></example>
    public static ITarGzStreamConfig CreateFor(Stream tarArchiveStream)
    {
        IInputStream inputStream = InputStream.CreateOver(tarArchiveStream);
        return new TarGzInputStreamConfig(inputStream);
    }
}
