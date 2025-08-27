using System;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace contoso.sqlite.raw.pgwriter
{
    /// <summary>Assembles the COPY command configuration and opens the connection to Postgres for the binary write operation.</summary>
    public class ConfiguredWriter
    {
        readonly Func<ReadContext, PostgresBinaryWriter, Task> _rowWriter;
        readonly WriterConfiguration _writerCfg;
        internal ConfiguredWriter(Func<ReadContext, PostgresBinaryWriter, Task> rowWriter, WriterConfiguration writerCfg)
        {
            _rowWriter = rowWriter;
            _writerCfg = writerCfg;
        }

        /// <summary>
        /// Opens the connection to Postgres and initializes the binary writer for the bulk COPY operation.
        /// This instance must be disposed by the caller when the operation is completed.
        /// </summary>
        public async Task<PostgresWriterContext> OpenAsync(CancellationToken cancelToken = default)
        {
            var pgConnection = new NpgsqlConnection(_writerCfg.ConnectionString);
            await pgConnection.OpenAsync(cancelToken).ConfigureAwait(false);
            NpgsqlBinaryImporter pgWriter = pgConnection.BeginBinaryImport(_writerCfg.CopyCommand);
            return new PostgresWriterContext(pgConnection, pgWriter, _rowWriter, cancelToken);
        }
    }
}
