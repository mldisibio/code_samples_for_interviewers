namespace contoso.utility.compression.zlib;

/// <summary>Represents a zlib header and footer to help with parsing.</summary>
public readonly struct ZLibFormatData
{
    /// <summary>First two bytes indicating the file was compressed by zlib using default compression.</summary>
    public readonly static byte[] ZLibSignature = new byte[] { 0x78, 0x9C };

    /// <summary>Assumes only compression mode and flags are present without FLG.DICT and DICTID. e.g. '0x789C'</summary>
    public const int ZLibHeaderLength = 2;
    /// <summary>A four-byte ADLER32 CRC.</summary>
    public const int ZLibFooterLength = 4;

    readonly static byte[] _emptyHeader = new byte[ZLibHeaderLength];
    readonly static byte[] _emptyFooter = new byte[ZLibFooterLength];

    /// <summary>Less common but alternative first two bytes combinations indicating the file was compressed by zlib using default compression.</summary>
    readonly static byte[][] _signatures = new byte[][]
        {
            new byte[]{ 0x78, 0x01 },
            new byte[]{ 0x78, 0x5E },
            new byte[]{ 0x78, 0x9C },
            new byte[]{ 0x78, 0xDA }
        };

    /// <summary>Initialize with a 2 byte header and 4 byte footer only.</summary>
    public ZLibFormatData(byte[] header, byte[] footer)
    {
        HasZLibSignature = ContainsZLibSignature(header);
        Header = header != null && header.Length >= ZLibHeaderLength ? header[..ZLibHeaderLength] : _emptyHeader;
        Footer = footer != null && footer.Length >= ZLibFooterLength ? footer[..ZLibFooterLength] : _emptyFooter;
    }

    /// <summary>An empty but initialized instance. Not the same as 'default' which is not initialized.</summary>
    public static ZLibFormatData Empty { get; } = new ZLibFormatData(_emptyHeader, _emptyFooter);

    /// <summary>
    /// True if initialized with one of several two-byte headers where the first byte is almost always (0x78).
    /// If false, all other fields are meaningless and simply extracted from zero-initialized header and footer byte arrays.
    /// </summary>
    public bool HasZLibSignature { get; init; }

    /// <summary>The two byte header with which this instance was initialized, or a zero-initialized array of two bytes if the header is not valid.</summary>
    public byte[] Header { get; init; }

    /// <summary>The four byte footer with which this instance was initialized, or a zero-initialized array of four bytes if the header is not valid.</summary>
    public byte[] Footer { get; init; }

    /// <summary>The first two bytes of the header. If header is valid, will start with the deflate signature (0x78)</summary>
    public ReadOnlySpan<byte> Signature => Header.AsSpan()[..2];

    /// <summary>Compression method and info stored in the first byte of a valid header.</summary>
    public byte CompressionMethod => Header.AsSpan()[0];

    /// <summary>Bits stored in the second byte of a valid header.</summary>
    public byte Flags => Header.AsSpan()[1];

    /// <summary>ADLER32 CRC checksum of the uncompressed data, stored in first four bytes of a valid footer.</summary>
    public ReadOnlySpan<byte> CRC => Footer.AsSpan()[..4];

    /// <summary>Printable display of the header bytes.</summary>
    public string HeaderDisplay => ((ReadOnlySpan<byte>)Header).TryConvertToHexString(out string? hexString, spaced: false) ? hexString : string.Empty;

    /// <summary>Printable display of the footer bytes.</summary>
    public string FooterDisplay => ((ReadOnlySpan<byte>)Footer).TryConvertToHexString(out string? hexString, spaced: false) ? hexString : string.Empty;

    /// <summary>True if the <paramref name="header"/> contains/starts-with an acceptable zlib signature.</summary>
    public static bool ContainsZLibSignature(byte[] header) 
        => header != null
        && header.Length >= ZLibSignature.Length
        && (header[..2].SequenceEqual(ZLibSignature[..2]) || _signatures.Any(sig => header[..2].SequenceEqual(sig[..2])));

}
