namespace contoso.utility.compression.deflate.entities;

internal readonly struct FirstPassResult
{
    public FirstPassResult(long totalRead, bool errorThrown) => (TotalRead, ErrorThrown) = (totalRead, errorThrown);
    public long TotalRead { get; init; }
    public bool ErrorThrown { get; init; }
}
