using System.Diagnostics.CodeAnalysis;

namespace contoso.utility.compression.deflate.entities;

internal readonly struct InflatedSegment
{
    public InflatedSegment(int index, int bytesRead, byte[] content)
        => (Index, BytesRead, Content) = (index, bytesRead, content ?? Array.Empty<byte>());

    public int Index { get; init; }

    public int BytesRead { get; init; }

    public byte[] Content { [return: NotNull] get; init; }
}
