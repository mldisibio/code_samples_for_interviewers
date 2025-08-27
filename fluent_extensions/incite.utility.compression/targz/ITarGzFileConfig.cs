
namespace contoso.utility.compression.targz;

/// <summary>Fluent configuration wrapper.</summary>
public interface ITarGzFileConfig
{
    /// <summary>Unzip and extract archived files and directories to the <paramref name="outputDirectory"/> path.</summary>
    /// <param name="outputDirectory">Full path the the output directory where the extracted contents will be written.</param>
    UnzipAndExtractToDirectory AndExtractToDirectory(string outputDirectory);

    /// <summary>Asynchronously unzip and extract archived files and directories to the <paramref name="outputDirectory"/> path.</summary>
    /// <param name="outputDirectory">Full path the the output directory where the extracted contents will be written.</param>
    UnzipAndExtractAsyncToDirectory AndExtractToDirectoryAsync(string outputDirectory);
}
