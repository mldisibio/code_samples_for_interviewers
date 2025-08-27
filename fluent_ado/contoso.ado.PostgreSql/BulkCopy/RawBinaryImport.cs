using System;
using System.IO;
using contoso.ado.Internals;
using Npgsql;

namespace contoso.ado.PostgreSql
{
    /// <summary>Wrapper providing fluent configuration for a specific bulk copy operation.</summary>
    public class RawBinaryImport<TIn>
    {
        readonly PostgresCommandContext _ctx;
        readonly Action<TIn, Stream> _writeData;

        internal RawBinaryImport(PostgresCommandContext cmdCtx, Action<TIn,Stream> writeData) { _ctx = cmdCtx; _writeData = writeData; }

        /// <summary>
        /// Invokes <see cref="M:Npgsql.NpgsqlConnection.BeginRawBinaryCopy"/> on the <see cref="NpgsqlConnection"/> in context
        /// <para>The underlying command text is of the format: "COPY tbl (col1, col2) FROM STDIN BINARY".</para>
        /// </summary>
        /// <param name="src">
        /// Placeholder for input data if needed. This can be the data to import into Postgres (as byte array or stream)
        /// or if the source is provided by the delegate handler, <typeparamref name="TIn"/> can be any discardable object.
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
                    using NpgsqlRawCopyStream writer = connection.BeginRawBinaryCopy(_ctx.Command.CommandText);
                    _writeData(src, writer);
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
