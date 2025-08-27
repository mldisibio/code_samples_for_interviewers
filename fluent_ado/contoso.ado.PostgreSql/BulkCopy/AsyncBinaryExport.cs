using System;
using System.Threading.Tasks;
using contoso.ado.Internals;
using Npgsql;

namespace contoso.ado.PostgreSql
{
    /// <summary>Wrapper providing fluent configuration for a specific bulk copy operation.</summary>
    public class AsyncBinaryExport<TOut>
    {
        readonly PostgresCommandContext _ctx;
        readonly Func<NpgsqlBinaryExporter, Task<TOut>> _readDataAsync;

        internal AsyncBinaryExport(PostgresCommandContext cmdCtx, Func<NpgsqlBinaryExporter, Task<TOut>> readDataAsync) { _ctx = cmdCtx; _readDataAsync = readDataAsync; }

        /// <summary>
        /// Invokes <see cref="M:Npgsql.NpgsqlConnection.BeginBinaryExport"/> on the <see cref="NpgsqlConnection"/> in context.
        /// <para>
        /// The underlying command text is of the format: "COPY tbl (col1, col2) TO STDIN BINARY".
        /// </para>
        /// </summary>
        public async Task<TOut> ReadBulkExportAsync()
        {
            ParamCheck.Assert.IsNotNull(_ctx, "PostgresCommandContext");
            ParamCheck.Assert.IsNotNull(_readDataAsync, "readDataAsync");
            TOut result;
            try
            {
                using (var connection = _ctx.CreateNpgsqlConnection())
                {
                    // although the 'export' and 'import' functions are driven from the connection, not the command
                    // our api is command driven, so we will simply read the command text from the command object
                    using NpgsqlBinaryExporter reader = connection.BeginBinaryExport(_ctx.Command.CommandText);
                    result = await _readDataAsync(reader);
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
