using System;

namespace contoso.sqlite.raw
{
    /// <summary>Context for executing a query and processing each result row as a task.</summary>
    public sealed class ProcessContext : OperationContext
    {
        readonly StatementContext _stmtCtx;
        readonly Action<ReadContext> _processRow;

        internal ProcessContext(StatementContext ctx, Action<ReadContext> processRow)
            : base(ctx)
        {
            _stmtCtx = ctx;
            _processRow = processRow;
        }

        /// <summary>Execute the prepared statement in context and process each row using the configured delegate.</summary>
        public void ExecuteReader()
        {
            ThrowIfDisposed();
            Statement.ThrowIfInvalid();
            if (_processRow == null)
                return;

            // the ReadContext for mapping output to result
            // but can only be initialized once we are sure we have a row context
            ReadContext? readCtx = null;
            _stmtCtx.StartTicks();

            // bind parameters to runtime values
            _stmtCtx.BindParametersToValuesIfAny();
            try
            {
                // execute the statement
                int result = SQLitePCL.raw.sqlite3_step(Statement);
                // iterate result rows and map result to output item
                while (result == SQLitePCL.raw.SQLITE_ROW)
                {
                    // see if row context was already evaluated after at least one row has been returned;
                    // a row context (column names and indices) won't change for a given statement
                    _stmtCtx.RowContext ??= new RowContext(this);
                    readCtx ??= new ReadContext(_stmtCtx.RowContext);
                    // apply caller supplied delegate to process the current row
                    _processRow(readCtx);
                    // move on to next row, if any
                    result = SQLitePCL.raw.sqlite3_step(Statement);
                }
                // after iterating all the rows, or if there were no rows
                if (result == SQLitePCL.raw.SQLITE_DONE)
                {
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
    }
}
