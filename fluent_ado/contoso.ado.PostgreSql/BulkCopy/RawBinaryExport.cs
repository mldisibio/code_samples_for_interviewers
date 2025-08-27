using System;
using System.IO;
using contoso.ado.Internals;
using Npgsql;

namespace contoso.ado.PostgreSql
{
    /// <summary>Wrapper providing fluent configuration for a specific bulk copy operation.</summary>
    public class RawBinaryExport<TOut>
    {
        readonly PostgresCommandContext _ctx;
        readonly Func<Stream, TOut> _readData;

        internal RawBinaryExport(PostgresCommandContext cmdCtx, Func<Stream, TOut> readData) { _ctx = cmdCtx; _readData = readData; }

        /// <summary>
        /// Invokes <see cref="M:Npgsql.NpgsqlConnection.BeginRawBinaryCopy"/> on the <see cref="NpgsqlConnection"/> in context
        /// <para>The underlying command text is of the format: "COPY tbl (col1, col2) TO STDIN BINARY".</para>
        /// </summary>
        public TOut ReadBulkExport()
        {
            ParamCheck.Assert.IsNotNull(_ctx, "PostgresCommandContext");
            ParamCheck.Assert.IsNotNull(_readData, "readData");
            TOut result;
            try
            {
                using (var connection = _ctx.CreateNpgsqlConnection())
                {
                    // although the 'export' and 'import' functions are driven from the connection, not the command
                    // our api is command driven, so we will simply read the command text from the command object
                    using NpgsqlRawCopyStream reader = connection.BeginRawBinaryCopy(_ctx.Command.CommandText);
                    result = _readData(reader);
                }
                _ctx.ContextLog.CommandExecuted(_ctx.Command);
                return result;
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
