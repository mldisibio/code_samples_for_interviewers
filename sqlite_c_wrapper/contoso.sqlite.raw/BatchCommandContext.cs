using System;
using System.Collections.Generic;
using System.Linq;

namespace contoso.sqlite.raw
{
    /// <summary>Execute a command in one or more transaction batches.</summary>
    public class BatchCommandContext<T> : OperationContext
    {
        readonly WriteStatementContext _stmtCtx;
        readonly Action<BindContext, T> _mapParamsFromEach;

        internal BatchCommandContext(WriteStatementContext stmtCtx, Action<BindContext, T> mapParams)
            : base(stmtCtx)
        {
            _stmtCtx = stmtCtx;
            _mapParamsFromEach = mapParams;
        }

        /// <summary>Execute a command in one or more transaction batches.</summary>
        public int ExecuteOver(IEnumerable<T> source, int? batchSize = 1024)
        {
            ThrowIfDisposed();
            Statement.ThrowIfInvalid();

            source ??= Enumerable.Empty<T>();
            // if no batch size specified, execute in one batch
            batchSize = batchSize.GetValueOrDefault() <= 0 ? int.MaxValue : batchSize!.Value;
            long? totalElapsed = 0;
            int rowsAffected = 0;
            int batchCounter = 0;
            int itemCounter = 0;

            BindContext? bindContext = CreateBindContext();
            // bit of a hack; sqlite3_changes() only reports I/U/D affected count, not on commit of a batch;
            //                sqlite3_total_changes() reports total number affected from I/U/D completed since the database connection was opened;
            //                we don't want to query sqlite3_changes() for each individual statement, and we don't want to simply count the statements;
            //                so subtract the total-before from the total-after, since our write connection is locked;
            int startingTotalChanges = SQLitePCL.raw.sqlite3_total_changes(base.DbHandle);

            // split source collection into batches
            var allBatches = source.TakeBy(batchSize.Value);
            // execute each batch in a transaction
            foreach (IEnumerable<T> transactionBatch in allBatches)
            {
                _stmtCtx.WriteConnection.StartTicks();
                // the BeginTransaction method will rollback if it fails
                _stmtCtx.WriteConnection.BeginTransaction();
                try
                {
                    // execute statement (e.g. INSERT/UPDATE/DELTE) once per source item
                    foreach (T item in transactionBatch)
                    {
                        // bind parameters to runtime values, if any
                        if (bindContext != null)
                            BindParametersToValuesFrom(item, bindContext);
                        try
                        {
                            // execute the statement
                            int result = SQLitePCL.raw.sqlite3_step(Statement);
                            // we are not expecting data back, but account for statements that might return a result value
                            if (!(result == SQLitePCL.raw.SQLITE_DONE || result == SQLitePCL.raw.SQLITE_ROW || result == SQLitePCL.raw.SQLITE_OK))
                                throw FailedExecution(result);
                            itemCounter++;
                        }
                        finally
                        {
                            base.ResetStatement();
                        }
                    }

                    // commit the batch; the CommitTransaction method will rollback if it fails
                    _stmtCtx.WriteConnection.CommitTransaction();
                    // query number of rows affected
                    rowsAffected = SQLitePCL.raw.sqlite3_total_changes(base.DbHandle) - startingTotalChanges;
                    // log execution
                    long? batchElapsed = _stmtCtx.WriteConnection.GetElapsed();
                    batchCounter++;
                    totalElapsed += batchElapsed.GetValueOrDefault();
                    SqliteDatabase.LogQueue.CommandExecuted($"Batch {$"{batchCounter}",4} [{$"{itemCounter:N0}",12}] {base.Sql}", batchElapsed);
                }
                catch (SqliteTransactionException)
                {
                    // transaction exceptions mean the transaction was already rolled back and the exception was logged
                    throw;
                }
                catch (SqliteDbException)
                {
                    _stmtCtx.WriteConnection.RollbackTransaction();
                    throw;
                }
                catch (Exception otherEx)
                {
                    var traceEx = FailedExecution(resultCode: ResultCodes.NonSqliteException, innerException: otherEx);
                    _stmtCtx.WriteConnection.RollbackTransaction();
                    throw traceEx;
                }
                finally
                {
                    _stmtCtx.WriteConnection.StopTicks();
                }
            } // end all transaction batches

            SqliteDatabase.LogQueue.CommandExecuted($"Batch Complete [{rowsAffected:N0}]", totalElapsed);
            return rowsAffected;
        }

        BindContext? CreateBindContext()
        {
            if (_stmtCtx.HasParameters)
            {
                // clear values of any previously bound parameters at start of execution
                int result = SQLitePCL.raw.sqlite3_clear_bindings(Statement);
                if (result != SQLitePCL.raw.SQLITE_OK)
                    throw FailedExecution(result);
                if (_mapParamsFromEach != null)
                    return new BindContext(_stmtCtx);
            }
            return null;
        }

        void BindParametersToValuesFrom(T src, BindContext bindCtx)
        {
            // clear values of any previously bound parameters
            int result = SQLitePCL.raw.sqlite3_clear_bindings(Statement);
            if (result != SQLitePCL.raw.SQLITE_OK)
                throw FailedExecution(result);

            // bind parameters to runtime values, if any
            _mapParamsFromEach(bindCtx, src);
        }
    }
}
