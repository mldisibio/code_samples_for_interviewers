using System.Runtime.CompilerServices;
using System.Text;
using contoso.sqlite.raw.stringutils;

namespace contoso.sqlite.raw
{
    /// <summary>Wraps the index for a specific column within a given sqlite result row.</summary>
    /// <remarks>Primarily for supporting a fluent data reader syntax.</remarks>
    public sealed class ColumnContext : OperationContext
    {
        /// <summary>Initialize with the zero-based result column index, and the current operation context.</summary>
        internal ColumnContext(int colIdx, OperationContext ctx)
            : base(ctx)
        {
            Index = colIdx;
        }

        /// <summary>The zero-based column index within <see cref="OperationContext.Statement"/>.</summary>
        public int Index { get; }
    }

    /// <summary>Fluent methods for reading a column from a sqlite row as a strongly typed value.</summary>
    public static class ColumnContextExtensions
    {
        readonly static UTF8Encoding _utf8 = new UTF8Encoding();
        readonly static byte[] _zeroBytes = Array.Empty<byte>();

        /// <summary>Read a column as a <see cref="Boolean"/> value.</summary>
        public static bool AsBoolean(this ColumnContext ctx) => AsInt32(ctx) > 0;
        /// <summary>Read a column as a <see cref="Byte"/> value.</summary>
        public static byte AsByte(this ColumnContext ctx) => (byte)AsInt32(ctx);
        /// <summary>Read a column as a <see cref="SByte"/> value.</summary>
        public static sbyte AsSByte(this ColumnContext ctx) => (sbyte)AsInt32(ctx);
        /// <summary>Read a column as a <see cref="Int16"/> value.</summary>
        public static short AsInt16(this ColumnContext ctx) => (short)AsInt32(ctx);
        /// <summary>Read a column as a <see cref="UInt16"/> value.</summary>
        public static ushort AsUInt16(this ColumnContext ctx) => (ushort)AsInt32(ctx);

        /// <summary>Read a column as a <see cref="Int32"/> value.</summary>
        public static int AsInt32(this ColumnContext ctx)
        {
            ctx.Statement.ThrowIfInvalid(ctx.FilePath);

            int colType = SQLitePCL.raw.sqlite3_column_type(ctx.Statement, ctx.Index);
            if (colType == SQLitePCL.raw.SQLITE_NULL)
                throw FailedRead(ResultCodes.NullColumnValue, ctx, colType);
            else if (colType == SQLitePCL.raw.SQLITE_INTEGER)
                return SQLitePCL.raw.sqlite3_column_int(ctx.Statement, ctx.Index);
            else if (colType == SQLitePCL.raw.SQLITE_TEXT)
            {
                string colValue = SQLitePCL.raw.sqlite3_column_text(ctx.Statement, ctx.Index).utf8_to_string();
                if (int.TryParse(colValue, out int parsed))
                    return parsed;
                else
                    SqliteDatabase.LogQueue.TraceError(null, $"Attempted to read value {colValue} as Int32.");
            }
            throw FailedRead(ResultCodes.InvalidColumnCast, ctx, colType);
        }

        /// <summary>Read a column as a nullable <see cref="Boolean"/> value.</summary>
        public static bool? AsNullableBoolean(this ColumnContext ctx) { int? val = AsNullableInt32(ctx); return val.HasValue ? val.Value > 0 : (bool?)null; }
        /// <summary>Read a column as a nullable <see cref="Byte"/> value.</summary>
        public static byte? AsNullableByte(this ColumnContext ctx) { int? val = AsNullableInt32(ctx); return val.HasValue ? (byte)val.Value : (byte?)null; }
        /// <summary>Read a column as a nullable <see cref="SByte"/> value.</summary>
        public static sbyte? AsNullableSByte(this ColumnContext ctx) { int? val = AsNullableInt32(ctx); return val.HasValue ? (sbyte)val.Value : (sbyte?)null; }
        /// <summary>Read a column as a nullable <see cref="Int16"/> value.</summary>
        public static short? AsNullableInt16(this ColumnContext ctx) { int? val = AsNullableInt32(ctx); return val.HasValue ? (short)val.Value : (short?)null; }
        /// <summary>Read a column as a nullable <see cref="UInt16"/> value.</summary>
        public static ushort? AsNullableUInt16(this ColumnContext ctx) { int? val = AsNullableInt32(ctx); return val.HasValue ? (ushort)val.Value : (ushort?)null; }

        /// <summary>Read a column as a nullable <see cref="Int32"/> value.</summary>
        public static int? AsNullableInt32(this ColumnContext ctx)
        {
            ctx.Statement.ThrowIfInvalid(ctx.FilePath);

            int colType = SQLitePCL.raw.sqlite3_column_type(ctx.Statement, ctx.Index);
            if (colType == SQLitePCL.raw.SQLITE_NULL)
                return null;
            else if (colType == SQLitePCL.raw.SQLITE_INTEGER)
                return SQLitePCL.raw.sqlite3_column_int(ctx.Statement, ctx.Index);
            else if (colType == SQLitePCL.raw.SQLITE_TEXT)
            {
                string colValue = SQLitePCL.raw.sqlite3_column_text(ctx.Statement, ctx.Index).utf8_to_string();
                if (int.TryParse(colValue, out int parsed))
                    return parsed;
                else
                    SqliteDatabase.LogQueue.TraceError(null, $"Attempted to read value {colValue} as nullable Int32.");
            }
            throw FailedRead(ResultCodes.InvalidColumnCast, ctx, colType);
        }

        /// <summary>Read a column as a <see cref="UInt32"/> value.</summary>
        public static uint AsUInt32(this ColumnContext ctx) => (uint)AsInt64(ctx);

        /// <summary>Read a column as a <see cref="Int64"/> value.</summary>
        public static long AsInt64(this ColumnContext ctx)
        {
            ctx.Statement.ThrowIfInvalid(ctx.FilePath);

            int colType = SQLitePCL.raw.sqlite3_column_type(ctx.Statement, ctx.Index);
            if (colType == SQLitePCL.raw.SQLITE_NULL)
                throw FailedRead(ResultCodes.NullColumnValue, ctx, colType);
            else if (colType == SQLitePCL.raw.SQLITE_INTEGER)
                return SQLitePCL.raw.sqlite3_column_int(ctx.Statement, ctx.Index);
            else if (colType == SQLitePCL.raw.SQLITE_TEXT)
            {
                string colValue = SQLitePCL.raw.sqlite3_column_text(ctx.Statement, ctx.Index).utf8_to_string();
                if (long.TryParse(colValue, out long parsed))
                    return parsed;
                else
                    SqliteDatabase.LogQueue.TraceError(null, $"Attempted to read value {colValue} as Int64.");
            }
            throw FailedRead(ResultCodes.InvalidColumnCast, ctx, colType);
        }

        /// <summary>Read a column as a nullable <see cref="UInt32"/> value.</summary>
        public static uint? AsNullableUInt32(this ColumnContext ctx) { long? val = AsNullableInt64(ctx); return val.HasValue ? (uint)val.Value : (uint?)null; }

        /// <summary>Read a column as a nullable <see cref="Int64"/> value.</summary>
        public static long? AsNullableInt64(this ColumnContext ctx)
        {
            ctx.Statement.ThrowIfInvalid(ctx.FilePath);

            int colType = SQLitePCL.raw.sqlite3_column_type(ctx.Statement, ctx.Index);
            if (colType == SQLitePCL.raw.SQLITE_NULL)
                return null;
            else if (colType == SQLitePCL.raw.SQLITE_INTEGER)
                return SQLitePCL.raw.sqlite3_column_int(ctx.Statement, ctx.Index);
            else if (colType == SQLitePCL.raw.SQLITE_TEXT)
            {
                string colValue = SQLitePCL.raw.sqlite3_column_text(ctx.Statement, ctx.Index).utf8_to_string();
                if (long.TryParse(colValue, out long parsed))
                    return parsed;
                else
                    SqliteDatabase.LogQueue.TraceError(null, $"Attempted to read value {colValue} as nullable Int64.");
            }
            throw FailedRead(ResultCodes.InvalidColumnCast, ctx, colType);
        }

        /// <summary>Read a column as a <see cref="Single"/> value.</summary>
        public static float AsSingle(this ColumnContext ctx) => (float)AsDouble(ctx);
        /// <summary>Read a column as a <see cref="Decimal"/> value.</summary>
        public static decimal AsDecimal(this ColumnContext ctx) => (decimal)AsDouble(ctx);

        /// <summary>Read a column as a <see cref="Double"/> value.</summary>
        public static double AsDouble(this ColumnContext ctx)
        {
            ctx.Statement.ThrowIfInvalid(ctx.FilePath);

            int colType = SQLitePCL.raw.sqlite3_column_type(ctx.Statement, ctx.Index);
            if (colType == SQLitePCL.raw.SQLITE_NULL)
                throw FailedRead(ResultCodes.NullColumnValue, ctx, colType);
            else if (colType == SQLitePCL.raw.SQLITE_FLOAT)
                return SQLitePCL.raw.sqlite3_column_double(ctx.Statement, ctx.Index);
            else if (colType == SQLitePCL.raw.SQLITE_TEXT)
            {
                string colValue = SQLitePCL.raw.sqlite3_column_text(ctx.Statement, ctx.Index).utf8_to_string();
                if (double.TryParse(colValue, out double parsed))
                    return parsed;
                else
                    SqliteDatabase.LogQueue.TraceError(null, $"Attempted to read value {colValue} as Double.");
            }
            throw FailedRead(ResultCodes.InvalidColumnCast, ctx, colType);
        }

        /// <summary>Read a column as a nullable <see cref="Single"/> value.</summary>
        public static float? AsNullableSingle(this ColumnContext ctx) { double? val = AsNullableDouble(ctx); return val.HasValue ? (float)val.Value : (float?)null; }
        /// <summary>Read a column as a nullable <see cref="Decimal"/> value.</summary>
        public static decimal? AsNullableDecimal(this ColumnContext ctx) { double? val = AsNullableDouble(ctx); return val.HasValue ? (decimal)val.Value : (decimal?)null; }

        /// <summary>Read a column as a nullable <see cref="Double"/> value.</summary>
        public static double? AsNullableDouble(this ColumnContext ctx)
        {
            ctx.Statement.ThrowIfInvalid(ctx.FilePath);

            int colType = SQLitePCL.raw.sqlite3_column_type(ctx.Statement, ctx.Index);
            if (colType == SQLitePCL.raw.SQLITE_NULL)
                return null;
            else if (colType == SQLitePCL.raw.SQLITE_FLOAT)
                return SQLitePCL.raw.sqlite3_column_double(ctx.Statement, ctx.Index);
            else if (colType == SQLitePCL.raw.SQLITE_TEXT)
            {
                string colValue = SQLitePCL.raw.sqlite3_column_text(ctx.Statement, ctx.Index).utf8_to_string();
                if (double.TryParse(colValue, out double parsed))
                    return parsed;
                else
                    SqliteDatabase.LogQueue.TraceError(null, $"Attempted to read value {colValue} as nullable Double.");
            }
            throw FailedRead(ResultCodes.InvalidColumnCast, ctx, colType);
        }

        /// <summary>Read a column as a <see cref="Guid"/> value from a correctly formatted string.</summary>
        public static Guid AsGuid(this ColumnContext ctx) => AsNullableGuid(ctx) ?? throw FailedRead(ResultCodes.NullColumnValue, ctx);

        /// <summary>Read a column as a nullable <see cref="Guid"/> value from a correctly formatted string.</summary>
        public static Guid? AsNullableGuid(this ColumnContext ctx) { string? val = AsString(ctx, strict: true); return val == null ? (Guid?)null : new Guid(val); }

        /// <summary>Read a column as a <see cref="Uri"/>.</summary>
        public static Uri? AsUri(this ColumnContext ctx) { string? val = AsString(ctx, strict: true); return val == null ? null : new UriBuilder(val).Uri; }

        /// <summary>Read a column as a string value. Value can be null.</summary>
        /// <param name="ctx">The column in context.</param>
        /// <param name="strict">
        /// True to throw an exception if the storage type is not 'text' or null. 
        /// False (default) to simply convert the value of any storage type to a string (byte arrays converted to a hex string).
        /// </param>
        public static string? AsString(this ColumnContext ctx, bool strict = false)
        {
            ctx.Statement.ThrowIfInvalid(ctx.FilePath);

            int colType = SQLitePCL.raw.sqlite3_column_type(ctx.Statement, ctx.Index);
            if (colType == SQLitePCL.raw.SQLITE_NULL)
                return null;
            if (strict && colType != SQLitePCL.raw.SQLITE_TEXT)
                throw FailedRead(ResultCodes.InvalidColumnCast, ctx, colType);

            return ReadAnyAsText(ctx.Statement, ctx.Index, out _);
        }

        /// <summary>Read a blob column as a byte array. Value can be null.</summary>
        public static byte[] AsByteArray(this ColumnContext ctx) { ReadOnlySpan<byte> val = AsByteSpan(ctx); return val.ToArray(); }

        /// <summary>Read a blob column as a <see cref="ReadOnlySpan{Byte}"/>. Value can be null.</summary>
        /// <remarks>A null <see cref="ReadOnlySpan{Byte}"/> has a Length of zero and can yield a zero byte array when 'ToArray()' is invoked.</remarks>
        public static ReadOnlySpan<byte> AsByteSpan(this ColumnContext ctx)
        {
            ctx.Statement.ThrowIfInvalid(ctx.FilePath);

            int colType = SQLitePCL.raw.sqlite3_column_type(ctx.Statement, ctx.Index);
            if (colType == SQLitePCL.raw.SQLITE_NULL)
                return null;
            else if (colType == SQLitePCL.raw.SQLITE_BLOB)
                return SQLitePCL.raw.sqlite3_column_blob(ctx.Statement, ctx.Index);

            throw FailedRead(ResultCodes.InvalidColumnCast, ctx, colType);
        }

        /// <summary>
        /// Extends an <see cref="OperationContext"/> to read a column value as text, remove any non-printable or line-breaking characters,
        /// and ensure the string is properly escaped (enclosed in quotes) for csv. Byte arrays are returned as a plain hex string.
        /// </summary>
        internal static string? ForCsv(this OperationContext ctx, int colIdx) => ForCsv(ctx.Statement, colIdx);

        /// <summary>Encodes the cleaned csv value as UTF8 bytes. Returns a zero byte array if the value is null.</summary>
        internal static byte[] ForCsvStream(this OperationContext ctx, int colIdx)
        {
            string? csv = ForCsv(ctx.Statement, colIdx);
            return csv == null ? _zeroBytes : _utf8.GetBytes(csv);
        }

        /// <summary>
        /// Allows the <see cref="ReadContext"/> to parse a <see cref="ColumnContext"/> inline as text, remove any non-printable or line-breaking characters,
        /// and ensure the string is properly escaped (enclosed in quotes) for csv. Byte arrays are returned as a plain hex string.
        /// </summary>
        public static string? ForCsv(this ColumnContext ctx) => ForCsv(ctx.Statement, ctx.Index);

        /// <summary>
        /// Read the curren column value as text, removes any non-printable or line-breaking characters,
        /// ensures the string is properly escaped (enclosed in quotes) for csv, and returns the string as a UTF8 encoded byte array.
        /// Returns a zero byte array if the value is null.
        /// </summary>
        public static byte[] ForCsvStream(this ColumnContext ctx)
        {
            string? csv = ForCsv(ctx.Statement, ctx.Index);
            return csv == null ? _zeroBytes : _utf8.GetBytes(csv);
        }

        /// <summary>
        /// Extends the <see cref="ReadContext"/> to parse a <see cref="ColumnContext"/> inline as text, remove any non-printable or line-breaking characters,
        /// and ensure the string is properly escaped (enclosed in quotes) for csv. Byte arrays are returned as a plain hex string.
        /// </summary>
        static string? ForCsv(SQLitePCL.sqlite3_stmt stmt, int idx)
        {
            // sqlite3_column_text is very forgiving and will convert any internal storage type to text or null;
            // in fact, the sqlite code does this when returning the values as an array in the callback from sqlite3_exec;
            // if the internal type is NULL for any column (even if normally int, float, text, or blob) the result is null;
            // null will also be returned if an out-of-memory exception is encountered by sqlite3_column_text,
            // but there will also be sqlite3_errcode result if that is the case;
            // note: utf8_to_string() returns null if the string it wraps was a null pointer;
            // reading a blob (originally stored as byte[]) as text will return each byte converted to a char;
            // if the original bytes happen to be text, it will look like text, but if its truly binary, it will look garbled;
            // so we need to convert byte[] data to hex client side if not done by our query SELECT hex(col);
            // our primary concern here is not converting to text, but efficiently ensuring the text is clean and/or escaped for csv;
            // only text data needs to be checked
            int colType = SQLitePCL.raw.sqlite3_column_type(stmt, idx);
            // return if null
            if (colType == SQLitePCL.raw.SQLITE_NULL)
                return null;
            // return blob as a hex string
            if (colType == SQLitePCL.raw.SQLITE_BLOB)
                return SQLitePCL.raw.sqlite3_column_blob(stmt, idx).ToHexString();
            // otherwise, read the value as text
            string field = SQLitePCL.raw.sqlite3_column_text(stmt, idx).utf8_to_string();
            // if storage type was null or numeric (anything other than text (or blob)), simply return the text value
            if (field == null || field.Length == 0 || colType != SQLitePCL.raw.SQLITE_TEXT)
                return field;
            // our default approach to csv: remove unwanted characters except tabs and ensure it's properly escaped (enclosed in quotes) for csv if necessary
            return CleanForCsv(field, replaceTabs: false, removeCtlChars: true);
        }

        /// <summary>
        /// Extends the <see cref="ReadContext"/> to remove inline any non-printable or line-breaking characters from <paramref name="field"/>
        /// as read from a <see cref="ColumnContext"/> and ensure the string is properly escaped (enclosed in quotes) for csv.
        /// Use case would be when special formatting or processing is applied to the stored value by the reader first,
        /// such as tranforming ticks into a datetime string, or extracting a subset of text from a message.
        /// </summary>
        /// <param name="ctx">The column in context.</param>
        /// <param name="field">The database already converted to text and possibly formatted per business rules.</param>
        /// <param name="replaceTabs">True to replace tabs with a space. (Newlines are replaced with space by default). Default is to leave them in the text.</param>
        /// <param name="removeCtlChars">True to remove any remaining control characters (0-32,127) from the text (default). False to leave them in the text and escape the field.</param>
        public static string? FormattedForCsv(this ColumnContext ctx, string field, bool replaceTabs = false, bool removeCtlChars = true)
            => CleanForCsv(field, replaceTabs, removeCtlChars);

        /// <summary>Read a column of any storage type as text.</summary>
        static string? ReadAnyAsText(SQLitePCL.sqlite3_stmt stmt, int idx, out int colType)
        {
            // sqlite3_column_text is very forgiving and will convert any internal storage type to text or null; https://sqlite.org/c3ref/column_blob.html
            // in fact, the sqlite code does this when returning the values as an array in the callback from sqlite3_exec;
            // if the internal type is NULL for any column (even if normally int, float, text, or blob) the result is null;
            // null will also be returned if an out-of-memory exception is encountered by sqlite3_column_text,
            // but there will also be sqlite3_errcode result if that is the case;
            //
            // note: utf8_to_string() returns null if the string it wraps was a null pointer;
            //
            // when sqlite reads a blob (data stored as byte[]) as text, it will return each byte converted to a char;
            // if the original bytes happen to be text, it will look like text, but if its truly binary, it will look garbled;
            // so our convention, unlike sqlite default, is to convert the byte[] data to hex; (or this can be done already if the query is 'SELECT hex(col');
            colType = SQLitePCL.raw.sqlite3_column_type(stmt, idx);
            // return if null
            if (colType == SQLitePCL.raw.SQLITE_NULL)
                return null;
            // return blob as a hex string
            if (colType == SQLitePCL.raw.SQLITE_BLOB)
                return SQLitePCL.raw.sqlite3_column_blob(stmt, idx).ToHexString();
            // otherwise, read the value as text
            return SQLitePCL.raw.sqlite3_column_text(stmt, idx).utf8_to_string();
        }

        /// <summary>Our approach to csv. No line breaks allowed.</summary>
        /// <param name="field">The database already converted to text and possibly formatted per business rules.</param>
        /// <param name="replaceTabs">True to replace tabs with a space. (Newlines are replaced with space by default). Default is to leave them in the text.</param>
        /// <param name="removeCtlChars">True to remove any remaining control characters (0-32,127) from the text (default). False to leave them in the text and escape the field.</param>
        static string? CleanForCsv(string field, bool replaceTabs = false, bool removeCtlChars = true)
        {
            if (field == null || field.Length == 0)
                return field;
            if (removeCtlChars)
                // remove newlines, remove any control characters and then escape for csv without have to check again for those characters
                return field.NewlinesToSpace(replaceTabs).RemoveControlChars().TrimCsvField().EscapeForCsv(checkForCtlChars: false);
            else
                // remove newlines, and then escape for csv, leaving control characters in the text
                return field.NewlinesToSpace(replaceTabs).TrimCsvField().EscapeForCsv(checkForCtlChars: true);
        }

        static SqliteDbException FailedRead(int resultCode, ColumnContext ctx, int? columnType = null, [CallerMemberName] string methodName = "")
        {
            string errMsg = $"{resultCode}-{ResultCodes.Lookup[resultCode]} when reading column [{ctx?.Index}] of type {GetTypeName(columnType)} {methodName}";
            return new SqliteDbException(result: resultCode, msg: errMsg, filePath: ctx?.FilePath);
        }

        static string GetTypeName(int? sqliteColType)
        {
            if (sqliteColType.HasValue)
                return sqliteColType.Value switch
                {
                    SQLitePCL.raw.SQLITE_INTEGER => "SQLITE_INTEGER",
                    SQLitePCL.raw.SQLITE_FLOAT => "SQLITE_FLOAT",
                    SQLitePCL.raw.SQLITE_TEXT => "SQLITE_TEXT",
                    SQLitePCL.raw.SQLITE_BLOB => "SQLITE_BLOB",
                    SQLitePCL.raw.SQLITE_NULL => "SQLITE_NULL",
                    _ => "(Undefined)"
                };
            else
                return "(Not obtained)";
        }

        static string? ToHexString(this ReadOnlySpan<byte> bin)
        {
            if (bin == default || bin.IsEmpty)
                return null;
            StringBuilder sb = new StringBuilder(bin.Length * 2);
            foreach (byte b in bin)
                sb.Append($"{b:X2}");
            return sb.ToString();
        }
    }
}
