using System.Text;

namespace contoso.utility.compression.tar;

/// <summary>Reflects the expected structure of a tar entry header following the ustar format.</summary>
/// <remarks>
/// This is not a full-blown tar reader and many assumptions are made about the format of tar-ed Oasis logs.
/// Access level is public for debugging puposes only, given all the corruption we've encountered with Oasis logs.
/// Othewise, can be made internal.
/// </remarks>
public readonly struct TarHeaderBlock
{
	/// <summary>Block size of 512 bytes used by tar format.</summary>
	public const int BlockLength = 512;

	readonly static ASCIIEncoding _enc = new ASCIIEncoding();
	readonly static Memory<byte> _ustarBytes = new byte[] { (byte)'u', (byte)'s', (byte)'t', (byte)'a', (byte)'r' };
	const string _ustar = "ustar";
	const string _directoryTypeFlag = "5";
	const int _magicPosition = 257;
	const string _caf2Ext = ".caf2";

	readonly bool _notEmpty;
	readonly Memory<byte> _header;

	/// <summary>Intialize with a block of 512 bytes.</summary>
	public TarHeaderBlock(byte[]? block)
	{
		// start with basic validity check of whether we have the full 512 bytes;
		_notEmpty = block != null && block.Length == BlockLength;
		if (_notEmpty && block != null)
		{
			_header = block;
			// file name or directory
			Name = TrimmedAsMaybeCaf2(UpToNullChars(_header[0..100]));
			TypeFlag = UpToNullChars(_header[156..157]);
			HasName = !string.IsNullOrEmpty(Name);
			IsDirectory = HasName && (Name!.EndsWith('/') || string.Equals(TypeFlag, _directoryTypeFlag));
			// file size
			string? sizeOctal = UpToNullChars(_header[124..136]);
			Size = (HasName && !IsDirectory) ? TryParseSizeOctal(sizeOctal) : (long?)null;
			// --------------------------
			// the format defined here assumes the 'ustar' format which so far is what is produced on Oasis;
			// should that change, we will have to update or extend the reader (pax, gnu);
			// --------------------------
			// get the 5 byte value at 257 no matter what it is; expected to be 'ustar', but if the block is corrupted, alignment may be shifted;
			// note: some tar archives have 'ustar' followed by null bytes, while others have it followed by spaces
			Magic = UpToNullChars(_header[257..263]).TrimEnd().ToLower();
			// For help with corrupt tar entries that have extra bytes at the start of the header, find the actual index of 'ustar'
			int actualMagic = _header.Span.IndexOf(_ustarBytes.Span);
			// if the string 'ustar' is found but not in the correct position, set the offset value
			JunkOffset = (actualMagic >= 0 && actualMagic != _magicPosition) ? actualMagic - _magicPosition : null;
			// final validity check is the 512 bytes with the magic 'ustar' string in the correct location;
			ValidHeader = _notEmpty && string.Equals(_ustar, Magic);
		}
		else
		{
			_header = Array.Empty<byte>();
			Name = null;
			TypeFlag = null;
			HasName = false;
			IsDirectory = false;
			Size = null;
			Magic = null;
			JunkOffset = null;
			ValidHeader = false;
		}
	}

	/// <summary>000 - 099 [100]</summary>
	/// <remarks>Will be full path (relative to root of tar). Directories end with '/'.</remarks>
	public string? Name { get; }

	/// <summary>100 - 107 [008]</summary>
	public string? Mode => _notEmpty ? UpToNullChars(_header[100..108]) : null;

	/// <summary>108 - 115 [008]</summary>
	public string? Uid => _notEmpty ? UpToNullChars(_header[108..116]) : null;

	/// <summary>116 - 123 [008]</summary>
	public string? Gid => _notEmpty ? UpToNullChars(_header[116..124]) : null;

	/// <summary>124 - 135 [012]</summary>
	public long? Size { get; }

	/// <summary>136 - 147 [012]</summary>
	public string? MTime => _notEmpty ? UpToNullChars(_header[136..148]) : null;

	/// <summary>148 - 155 [008]</summary>
	public string? Chksum => _notEmpty ? UpToNullChars(_header[148..156]) : null;

	/// <summary>156 - 156 [001]</summary>
	/// <remarks>0 = Normal file; 5 = Directory</remarks>
	public string? TypeFlag { get; }

	/// <summary>157 - 256 [100]</summary>
	public string? LinkName => _notEmpty ? UpToNullChars(_header[157..257]) : null;

	/// <summary>257 - 262 [006]</summary>
	public string? Magic { get; }

	/// <summary>263 - 264 [002]</summary>
	public string? Version => _notEmpty ? UpToNullChars(_header[263..265]) : null;

	/// <summary>265 - 296 [032]</summary>
	public string? UName => _notEmpty ? UpToNullChars(_header[264..297]) : null;

	/// <summary>297 - 328 [032]</summary>
	public string? GName => _notEmpty ? UpToNullChars(_header[297..329]) : null;

	/// <summary>329 - 336 [008]</summary>
	public string? DevMajor => _notEmpty ? UpToNullChars(_header[329..337]) : null;

	/// <summary>337 - 344 [008]</summary>
	public string? DevMinor => _notEmpty ? UpToNullChars(_header[337..345]) : null;

	/// <summary>345 - 499 [155]</summary>
	public string? Prefix => _notEmpty ? UpToNullChars(_header[345..500]) : null;

	/// <summary>True if header was intialized with 512 bytes and the magic 'ustar' string is found at the position defined by the ustar format.</summary>
	public bool ValidHeader { get; }

	/// <summary>
	/// Bytes to skip if the magic string 'ustar' is found in the header, but not in its correct position.
	/// This is to help adjust alignment if junk characters are at the start of the header.
	/// </summary>
	public int? JunkOffset { get; }

	/// <summary>True if entry is not a directory, and has a non-empty name. Should have size but can be a zero-byte file.</summary>
	public bool HasFile => _notEmpty && HasName && !IsDirectory && Size.HasValue && Size.Value >= 0;

	/// <summary>True if entry is a directory.</summary>
	public bool IsDirectory { get; }

	bool HasName { get; }

	static string UpToNullChars(Memory<byte> src)
	{
		int nulIdx = src.Span.IndexOf((byte)'\0');
		return nulIdx >= 0
			   ? nulIdx == 0 ? string.Empty : _enc.GetString(src.Span[..nulIdx])
			   : _enc.GetString(src.Span);
	}

	static long? TryParseSizeOctal(string? val)
	{
		if (string.IsNullOrEmpty(val))
			return null;
		try { return Convert.ToInt64(val, 8); }
		catch { return null; }
	}

	// we expect almost all file names to be '.caf2', so trim the frequent junk characters after the name if it is
	static string TrimmedAsMaybeCaf2(string src)
	{
		if (string.IsNullOrEmpty(src))
			return src;

		int extensionStart = src.IndexOf(_caf2Ext);
		return extensionStart > -1 ? src[..(extensionStart + _caf2Ext.Length)] : src.TrimEnd();

	}

}
