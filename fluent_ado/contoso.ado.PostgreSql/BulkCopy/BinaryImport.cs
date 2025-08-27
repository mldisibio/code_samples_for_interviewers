using System;
using contoso.ado.Internals;
using Npgsql;

namespace contoso.ado.PostgreSql
{
    /// <summary>Wrapper providing fluent configuration for a specific bulk copy operation.</summary>
    public class BinaryImport<TIn>
    {
        readonly PostgresCommandContext _ctx;
        readonly Action<TIn, NpgsqlBinaryImporter> _writeData;

        internal BinaryImport(PostgresCommandContext cmdCtx, Action<TIn, NpgsqlBinaryImporter> writeData) { _ctx = cmdCtx; _writeData = writeData; }

        /// <summary>
        /// Invokes <see cref="M:Npgsql.NpgsqlConnection.BeginBinaryImport"/> on the <see cref="NpgsqlConnection"/> in context
        /// <para>The underlying command text is of the format: "COPY tbl (col1, col2) FROM STDIN BINARY".</para>
        /// This method will close the writer (Complete()) when the delegate finishes its iteration.
        /// </summary>
        /// <param name="src">
        /// Placeholder for input data if needed. This can be the data to import into Postgres (as a collection)
        /// or if the source is provided by the delegate handler, <paramref name="src"/> can be any discardable object.
        /// </param>
        public void WriteBulkInsert(TIn src)
        {
            ParamCheck.Assert.IsNotNull(_ctx, "PostgresCommandContext");
            ParamCheck.Assert.IsNotNull(_writeData, "writeData");

            try
            {
                using (var connection = _ctx.CreateNpgsqlConnection())
                {
                    // although the 'export' and 'import' functions are driven from the connection, not the command
                    // our api is command driven, so we will simply read the command text from the command object
                    using NpgsqlBinaryImporter writer = connection.BeginBinaryImport(_ctx.Command.CommandText);
                    _writeData(src, writer);
                    writer.Complete();
                }
                _ctx.ContextLog.CommandExecuted(_ctx.Command);
            }
            catch (Exception cmdEx)
            {
                _ctx.ContextLog.CommandFailed(_ctx.Command, cmdEx);
                throw;
            }
            finally
            {
                _ctx.Command.SafeDispose();
            }
        }
    }
}
