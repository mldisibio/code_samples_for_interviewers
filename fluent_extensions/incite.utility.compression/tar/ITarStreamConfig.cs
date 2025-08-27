
namespace contoso.utility.compression.tar;

/// <summary>Fluent configuration wrapper.</summary>
public interface ITarStreamConfig
{
    /// <summary>Extract archived files and directories to the <paramref name="outputDirectory"/> path.</summary>
    /// <param name="outputDirectory">Full path the the output directory where the extracted contents will be written.</param>
    ExtractToDirectory AndExtractToDirectory(string outputDirectory);

    /// <summary>Asynchronously extract archived files and directories to the <paramref name="outputDirectory"/> path.</summary>
    /// <param name="outputDirectory">Full path the the output directory where the extracted contents will be written.</param>
    ExtractAsyncToDirectory AndExtractToDirectoryAsync(string outputDirectory);
}
