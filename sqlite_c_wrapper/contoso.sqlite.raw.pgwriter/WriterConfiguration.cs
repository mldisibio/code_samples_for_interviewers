using contoso.sqlite.raw.stringutils;

namespace contoso.sqlite.raw.pgwriter
{
    /// <summary>Configure the Postgres COPY command and a delegate for writing each column from Sqlite to STDIN.</summary>
    public class WriterConfiguration
    {

        internal WriterConfiguration(string connectionString)
        {
            ConnectionString = $"{connectionString}Max Auto Prepare=64;Auto Prepare Min Usages=1;";
        }

        internal string ConnectionString { get; }

        internal string CopyCommand { get; private set; } = default!;

        /// <summary>Define the Postgres table to which data will be written.</summary>
        /// <param name="tableName">Required. The Postgres table to which data will be written.</param>
        /// <param name="schemaName">Required if the table schema is not 'public'.</param>
        /// <param name="columnList">Optional list of columns or subset of columns, in write order, into which source data will be copied.</param>
        public WriterConfiguration DefinePgTarget(string tableName, string schemaName = "public", IEnumerable<string>? columnList = null)
        {
            return DefinePgTarget(tableName, schemaName, columnList.AsDelimitedString(toLower: true));
        }

        /// <summary>Define the Postgres table to which data will be written.</summary>
        /// <param name="tableName">Required. The Postgres table to which data will be written.</param>
        /// <param name="schemaName">Required if the table schema is not 'public'.</param>
        /// <param name="columnCsv">Optional comma delimited string of columns or subset of columns, in write order, into which source data will be copied. Enclosing parens are not needed.</param>
        public WriterConfiguration DefinePgTarget(string tableName, string schemaName = "public", string? columnCsv = null)
        {
            if (tableName.IsNullOrEmptyString())
                throw new ArgumentException(message: "A table name is the minimum requirement for the COPY operation", paramName: nameof(tableName));
            if (columnCsv.IsNullOrEmptyString())
                CopyCommand = FormatCopyCommandWithTableOnly(tableName, schemaName ?? "public");
            else
            {
                string columnsOnly = columnCsv.Trim().TrimStart('(').TrimEnd(')');
                if (columnsOnly.IsNullOrEmptyString())
                    CopyCommand = FormatCopyCommandWithTableOnly(tableName, schemaName ?? "public");
                else
                    CopyCommand = FormatCopyCommandWithTableAndColumns(tableName, schemaName, columnsOnly);
            }
            return this;
        }

        /// <summary>
        /// Define a delegate in which the Sqlite <see cref="ReadContext"/> applies the Postgres <see cref="PostgresBinaryWriter"/>
        /// to output a strongly typed Sqlite value to the STDIN stream from which the data is written to Postgres.
        /// </summary>
        public ConfiguredWriter DefinePgRowWriter(Func<ReadContext, PostgresBinaryWriter, Task> rowWriter) => new ConfiguredWriter(rowWriter, this);

        static string FormatCopyCommandWithTableOnly(string tableName, string schemaName) => $"COPY {schemaName}.{tableName} FROM STDIN (FORMAT BINARY)";

        static string FormatCopyCommandWithTableAndColumns(string tableName, string schemaName, string columnCsv) => $"COPY {schemaName}.{tableName}({columnCsv}) FROM STDIN (FORMAT BINARY)";
    }
}
