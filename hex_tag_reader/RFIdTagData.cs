using contoso.extraction.rfid.tags.io;

namespace contoso.extraction.rfid.tags.models;

/// <summary>Device tag expanded from it's hex representation.</summary>
public sealed class RFIdTagData
{
    readonly static string _zeros32 = new string('0', 32);
    // the minified ascii set; simply subtract 47 from each ascii char starting at 48
    // allowing 0-9A-Z to be stored each in six bits instead of eight
    const int MODIFIED_ASCII_BITS = 6; // six bits per modified ascii char
    const int MODIFIED_ASCII_MIN = 48; // first ascii char of the set is '0' stored minified as '0x01'
    const int MODIFIED_ASCII_OFFSET = (MODIFIED_ASCII_MIN - 1); // (47) the offset to add to each six bit value to get back to the regular eight bit char value
    const byte MODIFIED_ASCII_NUL = 0;
    readonly byte[] _tag;

    RFIdTagData(string hex, long? parentId)
    {
        ParentId = parentId;
        Valid = ValidateHex(hex, out _tag);
    }

    /// <summary>
    /// Create binary representation from the sqlite hex tag data <paramref name="hex"/>.
    /// Instance will be empty and 'IsValid' will be false, if the binary data cannot be parsed correctly.
    /// </summary>
    /// <param name="hex">The hex representation of the decrypted tag blog.</param>
    /// <param name="parentId">Optional id linking this instance to the source row. Required when running extraction, but not for standalone usage of the api.</param>
    public static RFIdTagData FromHex(string hex, long? parentId)
    {

        return new RFIdTagData(hex, parentId).Parse().ValidateData();
    }

    /// <summary>Optional id linking this instance to the source row. Required when running extraction, but not for standalone usage of the api.</summary>
    public long? ParentId { get; private set; }

    /// <summary>The tag is encoded with a Unique Identifier (UID) that serves as the serial number for a device upon its creation.</summary>
    public string? InstrumentUid { get; private set; }

    /// <summary>Device catalog number.</summary>
    public string? PropertyA { get; private set; }

    /// <summary>serial number written upon insertion.</summary>
    public string? PropertyB
    {
        get; internal set;
    }

    /// <summary>Incremented when writing a locked timestamp or latest timestamp to the  tag.</summary>
    public int? PropertyC { get; private set; }

    /// <summary>Total time used incremented every 15 minutes if the most recent timestamp has not expired.</summary>
    public double? PropertyD { get; private set; }

    /// <summary>An extended stop time limit beyond the minimum time limit while not exceeding the maximum time limit.</summary>
    public int? PropertyE { get; private set; }

    /// <summary>Incremented with every .</summary>
    public int? PropertyF { get; private set; }

    /// <summary>Incremented with every .</summary>
    public int? PropertyG { get; private set; }

    /// <summary>Count of .</summary>
    public int? PropertyH { get; private set; }

    /// <summary>Accumulated number of seconds for .</summary>
    public int? PropertyI { get; private set; }

    /// <summary>Country .</summary>
    public int? PropertyJ { get; private set; }

    /// <summary>Model.</summary>
    public string? PropertyK { get; private set; }

    /// <summary>Model parameter.</summary>
    public int? PropertyL { get; private set; }

    /// <summary>True if tag parsed without error and basic data validity checks pass.</summary>
    public bool Valid { get; internal set; }

    /// <summary>Any error or reason for invalid flag.</summary>
    public string? Notes { get; internal set; }

    bool ValidateHex(string hex, out byte[] tagAsBytes)
    {
        tagAsBytes = Array.Empty<byte>();
        if (hex.StartsWith("ERROR"))
        {
            Notes = "Byte_To_HEX_Failure";
            return false;
        }
        if (hex.IsNullOrEmptyString() || hex.Length != 344)
        {
            Notes = $"HEX_Invalid_Len [{(hex.IsNullOrEmptyString() ? "0" : hex.Length.ToString())}]";
            return false;
        }
        if (hex.StartsWith(_zeros32))
        {
            Notes = "HEX_All_Zeros";
            return false;
        }
        if (!(hex.StartsWith(ReaderUtil.SlixsUidPrefix) || hex.StartsWith(ReaderUtil.DnaUidPrefix)))
        {
            string uid = hex.Substring(0, 8);
            Notes = $"HEX_Invalid_Uid_Prefix [{uid}]";
            return false;
        }
        if (hex.AsHexToByteArray(out tagAsBytes))
        {
            if (tagAsBytes.Length == 172)
                return true;
            else
            {
                Notes = "HEX_to_Byte_Failure";
                return false;
            }
        }
        return false;
    }

    RFIdTagData Parse()
    {
        if (Valid == false)
            return this;
        try
        {
            InstrumentUid = _tag.AsByteArrayToHex(0, 8);
            PropertyA = ParseA();
            PropertyB = ParseB();
            PropertyC = ParseC();
            PropertyD = ConvertTimeSegmentsToFractionalHours(ParseD());
            PropertyE = ParseE();
            PropertyF = ParseF();
            var (completedSealsCount, regraspAlarmsCount) = ParseCompleteAndAlarms();
            PropertyG = regraspAlarmsCount;
            PropertyH = completedSealsCount;
            PropertyI = ParseI();
            PropertyJ = ParseJ();
            PropertyK = ParseK();
            PropertyL = ParseL();
            Valid = true;
        }
        catch (Exception ex)
        {
            Valid = false;
            Notes = ex.AsShortMessage();
        }
        return this;
    }

    /// <summary>True if all required key fields have non-null values.</summary>
    RFIdTagData ValidateData()
    {
        if (!Valid)
            return this;

        if (InstrumentUid.IsNullOrEmptyString())
        {
            Notes = "TAG_UID_Empty";
            Valid = false;
            return this;
        }
        if (!(InstrumentUid.StartsWith(ReaderUtil.PrefixA) || InstrumentUid.StartsWith(ReaderUtil.PrefixB)))
        {
            Notes = $"TAG_UID_Invalid_Prefix [{InstrumentUid}]";
            Valid = false;
            return this;
        }
        if (PropertyA.IsNullOrEmptyString())
        {
            Notes = "TAG_A_Empty";
            Valid = false;
            return this;
        }
        // TODO decide if we should invalidate for missing or invalid code;
        return this;
    }

    string ParseA()
    {
        // 8 bytes across rows 15 (last), 16 (all four), 17 (first three)
        int start = ByteIndex(row: 14, offset: 3);
        ReadOnlySpan<byte> skuBytes = _tag.GetSlice(start, length: 8);
        // we have 64 bits;
        ulong stored64 = skuBytes.AsLexicalBytesToStored64();
        // ----------------------
        // the first four bits are 'foo'
        //ulong fooVal = stored64 >> 60;
        //int fooType = fooVal.AsStored64ToLexicalBytes()[0];
        // ----------------------
        // after the first four bits for 'device type' we have 60 bits for sku;
        return ParseMinAsciiStringFromBits(stored64, 10);
    }

    string ParseB()
    {
        // row 41 (3.5 bytes) and row 42 (4 bytes)
        // well skip the first four bits (model we get separately)
        int start = ByteIndex(row: 40, offset: 0);
        ReadOnlySpan<byte> snBytes = _tag.GetSlice(start, length: 8);
        ulong stored64 = snBytes.AsLexicalBytesToStored64();
        // after the first four bits for 'model', we have 60 bits for B;
        return ParseMinAsciiStringFromBits(stored64, 10);
    }

    // last byte of row 40
    int ParseC() => _tag[ByteIndex(row: 39, offset: 3)] & 0xFF;

    int ParseD()
    {
        int start = ByteIndex(row: 2, offset: 2);
        ReadOnlySpan<byte> totalTimeBytes = _tag.GetSlice(start, length: 2);
        return totalTimeBytes.AsLexicalBytesToStored16();
    }

    // second byte of row 3; 0xFF = '1111 1111'
    int ParseE() => _tag[ByteIndex(row: 2, offset: 1)] & 0xFF;

    // first byte of row 3; 0xFF = '1111 1111'
    int ParseReprocessingCounter => _tag[ByteIndex(row: 2, offset: 0)] & 0xFF;

    int ParseF()
    {
        // first two bytes of row 39;
        int start = ByteIndex(row: 38, offset: 0);
        ReadOnlySpan<byte> initiatedBytes = _tag.GetSlice(start, length: 2);
        // last 14 bits; 0x3FFF = '0011 1111 1111 1111'
        return initiatedBytes.AsLexicalBytesToStored16() & 0x3FFF;
    }

    (int CompletedCount, int AlarmsCount) ParseCompleteAndAlarms()
    {
        // there are two values stored across three bytes (24 bits) of row 40
        // however, in both java and c# we need to work with 32 bits
        int start = ByteIndex(row: 39, offset: 0);
        ReadOnlySpan<byte> srcBytes = _tag.GetSlice(start, length: 4);
        // if we want to zero out the last byte for clarity, shift right, then left again, which fills those rightmost bits with zero
        uint threeByteValue = (srcBytes.AsLexicalBytesToStored32() >> 8) << 8;
        // the first 14 bits are 'completed', so shift the 32 bits over by 18;
        // 0x3FFF = '0011 1111 1111 1111'
        uint completed = (threeByteValue >> 18) & 0x3FFF;
        // the last 10 bits of the three-byte set are 'alarms', so shift the 32 bits over by 8 
        uint regrasps = (threeByteValue >> 8) & 0x3FF;

        return ((int)completed, (int)regrasps);
    }

    int ParseI()
    {
        // last two bytes of row 39
        int start = ByteIndex(row: 38, offset: 2);
        ReadOnlySpan<byte> timerBytes = _tag.GetSlice(start, length: 2);
        // all 16 bits
        return (int)(timerBytes.AsLexicalBytesToStored16() & 0xFFFF);
    }

    int ParseJ()
    {
        // first byte of row 43; 0xFF = '1111 1111'
        byte countryByte = _tag[ByteIndex(row: 42, offset: 0)];
        byte countryFlag = (byte)(countryByte & 0xFF);
        return countryFlag;
    }

    string? ParseK()
    {
        // first byte of row 39
        byte modeByte = _tag[ByteIndex(row: 38, offset: 0)];
        // first two bits; 0x3 = '0011'
        byte shiftedVal = (byte)(modeByte >> 6);
        byte modeFlag = (byte)(shiftedVal & 0x3);
        return ToRFIdUsageModelString(modeFlag);
    }

    int ParseL()
    {
        // first byte of row 41, last three bits of first half;
        byte paramByte = _tag[ByteIndex(row: 40, offset: 0)];
        byte shiftedVal = (byte)(paramByte >> 4);
        //  0x7 = '0111'
        byte paramFlag = (byte)(shiftedVal & 0x7);
        return paramFlag;
    }

    /// <summary>
    /// Applies same minified ascii parsing to both A and B
    /// since both read ten characters from the last 60 bits of <paramref name="stored64"/>.
    /// </summary>
    static string ParseMinAsciiStringFromBits(ulong stored64, int wordLen)
    {
        var output = new List<char>(wordLen);
        // with modified ascii, each character is stored in six bits;
        // so shift by six for each char, and convert each set of six bits (captured as a byte) to a character;
        for (int i = 0; i < wordLen; i++)
        {
            ulong shiftedVal = stored64 >> (MODIFIED_ASCII_BITS * i);
            byte[] shifted = shiftedVal.AsStored64ToLexicalBytes();
            // 0x3F = '0011 1111'
            byte verified = (byte)(shifted.Last() & 0x3F);
            byte charVal = (byte)(verified + MODIFIED_ASCII_OFFSET);
            if (verified != MODIFIED_ASCII_NUL)
                output.Add((char)charVal);
        }
        // however, the characters are stored 'LittleEndian' (reversed lexical order)
        return new string(((IEnumerable<char>)output).Reverse().ToArray());
    }

    static double ConvertTimeSegmentsToFractionalHours(int fifteenMinuteSegments) => (double)(fifteenMinuteSegments) / 4;

    /// <summary>Return a text representation of the model byte value.</summary>
    public static string? ToRFIdUsageModelString(byte idx)
        => idx switch
        {
            0 => "alpha",
            1 => "beta",
            2 => "Single",
            3 => "Invalid",
            _ => null //throw new IndexOutOfRangeException()
        };

    /// <summary>
    /// Return the zero-based index of the byte at the zero-based <paramref name="row"/>
    /// optionally offset by (0,1,2,3) <paramref name="offset"/>.
    /// This corresponds to laying out our 172 byte hex string into 43 rows of four bytes each, left to right,
    /// as a way of visually mapping the data storage similar to the actual storage spec.
    /// </summary>
    static int ByteIndex(int row, int offset = 0) => (row * 4) + offset;

    /// <summary>The tag contents as a comma delimited string.</summary>
    public override string ToString()
        => $"{InstrumentUid},{PropertyB},{PropertyC},{PropertyD:F2},{PropertyE},{PropertyF},{PropertyG},{PropertyH},{PropertyI},{PropertyJ},{PropertyK},{PropertyL},{PropertyA}";

}
