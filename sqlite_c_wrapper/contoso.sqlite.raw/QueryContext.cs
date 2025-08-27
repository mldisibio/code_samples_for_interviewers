using System;
using System.Collections.Generic;

namespace contoso.sqlite.raw
{
    /// <summary>Context for executing a query and iterating its result rows.</summary>
    public sealed class QueryContext<T> : OperationContext
    {
        readonly StatementContext _stmtCtx;
        readonly Func<ReadContext, T?> _mapRow;

        internal QueryContext(StatementContext ctx, Func<ReadContext, T?> mapRow)
            : base(ctx)
        {
            _stmtCtx = ctx;
            _mapRow = mapRow ?? (_ => default);
        }

        /// <summary>Execute the prepared statement in context and map each row to an instance of <typeparamref name="T"/>.</summary>
        public IEnumerable<T?> ExecuteReader()
        {
            ThrowIfDisposed();
            Statement.ThrowIfInvalid();
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
                    // map current row to expected result
                    yield return _mapRow(readCtx);
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
                ResetStatement();
                _stmtCtx.StopTicks();
            }
        }
    }
}
