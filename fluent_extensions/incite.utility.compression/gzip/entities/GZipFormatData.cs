
namespace contoso.utility.compression.gzip;

/// <summary>Represents a gzip header and footer to help with parsing.</summary>
public readonly struct GZipFormatData
{
    /// <summary>First two bytes indicating the file was compressed by gzip.</summary>
    public readonly static ReadOnlyMemory<byte> GZipSignature = new byte[] { 0x1F, 0x8B };

    /// <summary>Single-file header length. e.g. '0x1F8B08000000000000FF'</summary>
    public const int GZipHeaderLength = 10;
    /// <summary>Single-file footer length encompassing a four-byte CRC and four-byte expected decompressed size.</summary>
    public const int GZipFooterLength = 8;

    readonly static byte[] _emptyHeader = new byte[GZipHeaderLength];
    readonly static byte[] _emptyFooter = new byte[GZipFooterLength];

    /// <summary>Initialize with a 10 byte header and 8 byte footer only.</summary>
    public GZipFormatData(byte[] header, byte[] footer)
    {
        HasGZipSignature = ContainsGZipSignature(header);
        Header = header != null && header.Length >= GZipHeaderLength ? header[..GZipHeaderLength] : _emptyHeader;
        Footer = footer != null && footer.Length >= GZipFooterLength ? footer[..GZipFooterLength] : _emptyFooter;
    }

    /// <summary>An empty but initialized instance. Not the same as 'default' which is not initialized.</summary>
    public static GZipFormatData Empty { get; } = new GZipFormatData(_emptyHeader, _emptyFooter);

    /// <summary>
    /// True if initialized with a header starting with the GZIP signature (0x1F8B).
    /// If false, all other fields are meaningless and simply extracted from zero-initialized header and footer byte arrays.
    /// </summary>
    public bool HasGZipSignature { get; init; }

    /// <summary>The ten byte header with which this instance was initialized, or a zero-initialized array of ten bytes if the header is not valid.</summary>
    public byte[] Header { get; init; }

    /// <summary>The eight byte footer with which this instance was initialized, or a zero-initialized array of eight bytes if the header is not valid.</summary>
    public byte[] Footer { get; init; }

    /// <summary>The first two bytes of the header. If header is valid, will be the GZIP signature (0x1F8B)</summary>
    public ReadOnlySpan<byte> Signature => Header.AsSpan()[..2];

    /// <summary>Compression method (0x08 = DEFLATE) stored in the third byte of a valid header.</summary>
    public byte CompressionMethod => Header.AsSpan()[2];

    /// <summary>Bit vector for extra fields stored in the fourth byte of a valid header.</summary>
    public byte Flag => Header.AsSpan()[3];

    /// <summary>Modification time (UNIX format) stored in bytes 5 through 8 of a valid header.</summary>
    public ReadOnlySpan<byte> Modified => Header.AsSpan().Slice(4, 4); // Header[4..8]

    /// <summary>Extra flags stored in ninth byte of a valid header.</summary>
    public byte ExtraFlag => Header.AsSpan()[8];

    /// <summary>OS stored in tenth byte of a valid header.</summary>
    public byte OS => Header.AsSpan()[9];

    /// <summary>CRC-32 checksum of the uncompressed data, stored in first four bytes of a valid footer.</summary>
    public ReadOnlySpan<byte> CRC => Footer.AsSpan()[..4];

    /// <summary>Size of the uncompressed data stored in last four bytes of a valid footer.</summary>
    public ReadOnlySpan<byte> Size => Footer.AsSpan()[^4..];

    /// <summary>
    /// The expected length, in bytes, of the uncompressed data. Not always reliable.
    /// If the actual size is greater than 4Gb, this value represents the length modulo 2^32.
    /// </summary>
    /// <remarks>Just a ballpark help for our library. Otherwise, cf https://stackoverflow.com/a/54360738/458354 </remarks>
    public uint? UncompressedLength
    {
        get
        {
            if (HasGZipSignature)
                try { return BitConverter.ToUInt32(Size); }
                catch { }
            return null;
        }
    }

    /// <summary>Printable display of the header bytes.</summary>
    public string HeaderDisplay => ((ReadOnlySpan<byte>)Header).TryConvertToHexString(out string? hexString, spaced: false) ? hexString : string.Empty;

    /// <summary>Printable display of the footer bytes.</summary>
    public string FooterDisplay => ((ReadOnlySpan<byte>)Footer).TryConvertToHexString(out string? hexString, spaced: false) ? hexString : string.Empty;

    /// <summary>True if instance is an uninitialized default.</summary>
    internal bool Uninitialized => Header == null;

    /// <summary>Convenience method to check if given <paramref name="header"/> starts with the GZIP signature (0x1F8B).</summary>
    public static bool ContainsGZipSignature(in ReadOnlySpan<byte> header) => header.Length >= GZipSignature.Length && header[..2].SequenceEqual(GZipSignature.Span);

    /// <summary>Printable display of the header and footer bytes.</summary>
    public override string ToString() => $"{HeaderDisplay}...{FooterDisplay}";

}