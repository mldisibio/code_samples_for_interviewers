using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using contoso.sqlite.raw.stringutils;

namespace contoso.sqlite.raw
{
    /// <summary>Wraps a sqlite3 result row and its column collection.</summary>
    public sealed class RowContext : OperationContext
    {
        readonly Dictionary<string, int> _columns;
        StringBuilder? _csvBuilder;
        readonly object _builderLock = new object();
        readonly static byte[] _lfBytes = new byte[] { 0x0A };
        readonly static byte[] _crlfBytes = new byte[] { 0x0D, 0x0A };

        internal RowContext(OperationContext ctx)
            : base(ctx)
        {
            try
            {
                ColumnCount = ExtractNamedColumns(base.Statement, out _columns);
            }
            catch (Exception ex)
            {
                throw new SqliteDbException(result: null, msg: "While extracting column names", filePath: FilePath, innerException: ex);
            }
        }

        /// <summary>Count of columns in the result set.</summary>
        public int ColumnCount { get; }

        /// <summary>Need to omit separater after last column.</summary>
        int LastColumnIndex => HasColumns ? ColumnCount - 1 : 0;

        /// <summary>True if the result set has columns.</summary>
        public bool HasColumns => ColumnCount > 0;

        /// <summary>Produce a comma separated list of the column names in this row, or an empty string if there are no columns.</summary>
        /// <param name="toLower">True if all column names should be converted to lower case. Default is to leave as-is.</param>
        public string CsvOfColumnNames(bool toLower = false) => ColumnNames.AsDelimitedString(delim: ",", toLower: toLower);

        /// <summary>The column names as a collection of string.</summary>
        public IEnumerable<string> ColumnNames => _columns.Keys.Cast<string>();

        /// <summary>Retrieve the index of <paramref name="colName"/> from the execution result row in context.</summary>
        public int this[string colName]
        {
            get
            {
                if (HasColumns)
                {
                    if (_columns.TryGetValue(colName, out int colIdx))
                        return colIdx;
                    else
                        throw new IndexOutOfRangeException($"Column [{colName}] not found");
                }
                else
                    throw new IndexOutOfRangeException($"Result has no columns");
            }
        }

        /// <summary>
        /// Returns the values from the current row formatted as a <paramref name="sep"/> delimited string with no newline.
        /// Every column in the current row is read as text and written as-is with our default csv cleaning rules applied.
        /// </summary>
        public string AsCsvLine(char sep = ',')
        {
            if (ColumnCount == 0)
                return string.Empty;

            lock (_builderLock)
            {
                WriteRowToBuilder(sep);
                return _csvBuilder!.ToString();
            }
        }

        /// <summary>
        /// Writes the values from the current row formatted as a <paramref name="sep"/> delimited string to <paramref name="stream"/> and appends <paramref name="newline"/>.
        /// Every column in the current row is read as text and written as-is with our default csv cleaning rules applied.
        /// </summary>
        public async Task<long> WriteCsvToStream(Stream stream, char sep = ',', string newline = "\n")
        {
            // if no data, write a line? mmm...no purpose really; more sensible to just return
            if (ColumnCount == 0)
                return 0;
            if (stream == null || !stream.CanWrite)
                throw new IOException("Cannot write row to closed stream");

            // supporting only CRLF and LF
            byte[] linefeed = newline?.Length == 2 ? _crlfBytes : _lfBytes;
            byte _sepByte = (byte)sep;

            long totalBytesWritten = 0;
            for (int colIdx = 0; colIdx < ColumnCount; colIdx++)
            {
                // write column value to stream
                byte[] csvBytes = this.ForCsvStream(colIdx);
                if (csvBytes.Length > 0)
                {
                    await stream.WriteAsync(csvBytes, cancellationToken: CancellationToken.None);
                    totalBytesWritten += csvBytes.Length;
                }
                // write separator
                if (colIdx < LastColumnIndex)
                {
                    stream.WriteByte(_sepByte);
                    totalBytesWritten += 1;
                }
            }
            // write newline
            stream.Write(linefeed, 0, linefeed.Length);
            totalBytesWritten += linefeed.Length;

            return totalBytesWritten;
        }

        void WriteRowToBuilder(char sep = ',')
        {
            lock (_builderLock)
            {
                EmptyCsvBuilder();

                for (int colIdx = 0; colIdx < ColumnCount; colIdx++)
                {
                    _csvBuilder!.Append(this.ForCsv(colIdx));
                    if (colIdx < LastColumnIndex)
                        _csvBuilder.Append(sep);
                }
            }
        }


        /// <summary>
        /// Creates a dictionary of column names and their indices, or an empty dictionary if <paramref name="stmt"/> has no parameter placeholders.
        /// Returns the number of columns.
        /// </summary>
        /// <remarks>
        /// Would be simple to also capture the datatype for each column, but since sqlite uses dynamic run-time typing, 
        /// at a minimum we need to check for datatype 'null', and in theory (per documentation),
        /// just because a column is declared to contain a particular type does not mean that the data stored in that column is of the declared type.
        /// </remarks>
        public static int ExtractNamedColumns(SQLitePCL.sqlite3_stmt stmt, out Dictionary<string, int> columnLookup)
        {
            stmt.ThrowIfInvalid();

            // find the count of columns;
            int columnCnt = SQLitePCL.raw.sqlite3_column_count(stmt);
            if (columnCnt > 0)
            {
                columnLookup = new Dictionary<string, int>(columnCnt);
                // column indices are 0-based;
                for (int cIdx = 0; cIdx < columnCnt; cIdx++)
                {
                    // find the name for each column
                    string colName = SQLitePCL.raw.sqlite3_column_name(stmt, cIdx).utf8_to_string();
                    columnLookup[colName] = cIdx;
                }
                return columnLookup.Count;
            }
            else
            {
                columnLookup = new Dictionary<string, int>(0);
                return 0;
            }
        }

        void EmptyCsvBuilder()
        {
            lock (_builderLock)
            {
                // attempt to pre-allocate enough space, but this is just a guess (64 char per column)
                _csvBuilder ??= new StringBuilder(ColumnCount * 64);
                // remove the content (leaves capacity the same)
                _csvBuilder.Length = 0;
            }
        }

        /// <summary>Clean up resources referenced by the <see cref="RowContext"/>.</summary>
        protected override void Dispose(bool isManagedCall)
        {
            if (!_isAlreadyDisposed && isManagedCall)
            {
                _columns?.Clear();
            }
            base.Dispose(isManagedCall);
        }
    }
}
