using System;

namespace contoso.sqlite.raw
{
    /// <summary>Execution context for a single statement not returning a resultset.</summary>
    public sealed class ScalarCommandContext : OperationContext
    {
        readonly StatementContext _stmtCtx;

        internal ScalarCommandContext(StatementContext ctx)
            : base(ctx)
        {
            _stmtCtx = ctx;
        }

        /// <summary>Execute the prepared statement in context and return the first column of the last row returned, as a scalar value.</summary>
        /// <param name="map">Supply a delegate to map the first column of the expected result to the requested scalar <typeparamref name="T"/>.</param>
        public T? ExecuteScalar<T>(Func<ColumnContext, T> map)
        {
            ThrowIfDisposed();
            Statement.ThrowIfInvalid();
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            var columnCtx = new ColumnContext(0, this);
            Exception? lastReadEx = null;
            T? scalar = default;

            _stmtCtx.StartTicks();
            // bind parameters to runtime values
            _stmtCtx.BindParametersToValuesIfAny();

            try
            {
                // execute the statement
                int result = SQLitePCL.raw.sqlite3_step(Statement);
                // iterate zero or more result rows
                // idea is to account for any intermediate results before the final scalar is returned
                while (result == SQLitePCL.raw.SQLITE_ROW)
                {
                    lastReadEx = null;
                    scalar = default;
                    try
                    {
                        // apply the delegate to map the first column to the requested scalar
                        // this might fail if there are intermediate rows of a different expected data type
                        // or even if the correct row is an unexpected data type
                        scalar = map(columnCtx);
                    }
                    catch (Exception ex)
                    {
                        lastReadEx = ex;
                    }
                    // move on to next row, if any
                    result = SQLitePCL.raw.sqlite3_step(Statement);
                }
                // after iterating all the rows, or if there were no rows
                if (result == SQLitePCL.raw.SQLITE_DONE)
                {
                    // if the last row read threw an exception (such as a cast exception)
                    // throw that error
                    if (lastReadEx != null)
                        throw lastReadEx;

                    // no pending exception
                    // log execution
                    SqliteDatabase.LogQueue.CommandExecuted(base.Sql, _stmtCtx.GetElapsed());
                    // return the last read value;
                    // this could be 'default' if there were no rows, which would not necessarily be an error
                    return scalar;
                }
                else
                {
                    // execution of the statement is neither ROW nor DONE
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
