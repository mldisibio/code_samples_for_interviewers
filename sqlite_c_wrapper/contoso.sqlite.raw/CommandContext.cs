using System;

namespace contoso.sqlite.raw
{
    /// <summary>Execution context for a single statement not returning a resultset.</summary>
    public sealed class CommandContext : OperationContext
    {
        readonly WriteStatementContext _stmtCtx;

        internal CommandContext(WriteStatementContext ctx)
            : base(ctx)
        {
            _stmtCtx = ctx;
        }

        /// <summary>Execute the prepared statement in context and return the number of rows affected.</summary>
        /// <param name="rowsAffected">Number of rows affected only if the statement is an 'INSERT', 'UPDATE', or 'DELETE'.</param>
        /// <param name="lastRowId">Last rowid if the statement is an 'INSERT'.</param>
        public void ExecuteNonQuery(out int rowsAffected, out long lastRowId)
        {
            ThrowIfDisposed();
            Statement.ThrowIfInvalid();

            rowsAffected = -1;
            lastRowId = -1;

            _stmtCtx.WriteConnection.InUseLock.Wait();
            try
            {
                _stmtCtx.StartTicks();
                // bind parameters to runtime values
                _stmtCtx.BindParametersToValuesIfAny();
                try
                {
                    // execute the statement
                    int result = SQLitePCL.raw.sqlite3_step(Statement);
                    // we are not expecting data back, but account for statements that might return a result value
                    if (result == SQLitePCL.raw.SQLITE_DONE || result == SQLitePCL.raw.SQLITE_ROW)
                    {
                        if (_stmtCtx.IsModifyStatement)
                            rowsAffected = SQLitePCL.raw.sqlite3_changes(base.DbHandle);
                        if (_stmtCtx.IsInsertStatement)
                            lastRowId = SQLitePCL.raw.sqlite3_last_insert_rowid(base.DbHandle);
                        // log execution
                        SqliteDatabase.LogQueue.CommandExecuted(base.Sql, _stmtCtx.GetElapsed());
                    }
                    else
                    {
                        // execution of the statement failed
                        throw FailedExecution(result);
                    }
                }
                finally
                {
                    base.ResetStatement();
                    _stmtCtx.StopTicks();
                }
            }
            finally
            {
                _stmtCtx.WriteConnection.InUseLock.Release();
            }
        }
    }
}
