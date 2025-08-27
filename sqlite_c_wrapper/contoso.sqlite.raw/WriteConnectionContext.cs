using System;
using System.Collections.Generic;

namespace contoso.sqlite.raw
{
    /// <summary>
    /// Wraps a read-write, opened sqlite database handle, any cached prepared statements created in context of the connection,
    /// and supports serializing access to the database so that the connection may be shared by one or more threads.
    /// </summary>
    public sealed class WriteConnectionContext : ConnectionContext
    {
        readonly Dictionary<SqlHash, WriteStatementContext> _preparedStmtCache;

        internal WriteConnectionContext(SQLitePCL.sqlite3 dbHandle, string filePath)
            : base(dbHandle, filePath)
        {
            _preparedStmtCache = new Dictionary<SqlHash, WriteStatementContext>();
        }

        /// <summary>
        /// Retrieve the already prepared statement for <paramref name="sql"/> from cache
        /// or invoke the native sqlite3_prepare_v2() method on <paramref name="sql"/>, caching the result for later reuse.
        /// </summary>
        protected override StatementContext GetOrPrepareCore(string sql) => GetOrPrepare(sql);

        /// <summary>
        /// Invoke the native sqlite3_prepare_v2() method on <paramref name="sql"/> without caching the prepared statement.
        /// Returns the full text of unparsed statements found after the first complete sql statement if <paramref name="sql"/>
        /// contains multiple statements.
        /// </summary>
        protected override StatementContext PrepareCore(string sql, out string? remainingSql) => Prepare(sql, out remainingSql);

        /// <summary>
        /// Retrieves the already prepared statement for <paramref name="sql"/> from cache
        /// or invokes the native sqlite3_prepare_v2() method on <paramref name="sql"/>, caching the result for later reuse.
        /// </summary>
        /// <remarks>
        /// Note that sqlite3_prepare_v2() only parses the first complete sql statement 
        /// (i.e. parsing and execution of multiple sql statements in one string is not supported without some sort of iteration.
        /// </remarks>
        public new WriteStatementContext GetOrPrepare(string sql)
        {
            // note to self: 'covariant return types in overrides' https://stackoverflow.com/a/5709191/458354
            //
            // locking, because preparation and use of a prepared statement handle must be a serial operation;
            // one connection, one statement at a time; a second prepare for the same sql would invalidate the first;
            InUseLock.Wait();
            try
            {
                var sqlHash = new SqlHash(sql);
                if (_preparedStmtCache.TryGetValue(sqlHash, out WriteStatementContext? ctx))
                {
                    if (ctx.Statement.SeemsValid())
                        return ctx;
                    else
                    {
                        TryFinalizeStatement(ctx.Statement);
                        _preparedStmtCache.Remove(sqlHash);
                    }
                }
                // will throw on failure
                SQLitePCL.sqlite3_stmt stmt = PrepareStatement(sql, out string? remainingSql);
                ctx = new WriteStatementContext(stmt, new SqlHash(sql, remainingSql), this);
                _preparedStmtCache[ctx.SqlHash] = ctx;
                return ctx;
            }
            finally
            {
                InUseLock.Release();
            }
        }

        /// <summary>
        /// Invokes the native sqlite3_prepare_v2() method on <paramref name="sql"/> without caching the prepared statement.
        /// Caller should dispose of the context when finished executing the statement.
        /// </summary>
        /// <remarks>
        /// Note that sqlite3_prepare_v2() only parses the first complete sql statement 
        /// (i.e. parsing and execution of multiple sql statements in one string is not supported without some sort of iteration.
        /// </remarks>
        public new WriteStatementContext Prepare(string sql) => Prepare(sql, out _);

        /// <summary>
        /// Invokes the native sqlite3_prepare_v2() method on <paramref name="sql"/> without caching the prepared statement.
        /// Also returns the full text of unparsed statements found after the first complete sql statement if <paramref name="sql"/>
        /// contains multiple statements.
        /// Caller should dispose of the context when finished executing the statement.
        /// </summary>
        public new WriteStatementContext Prepare(string sql, out string? remainingSql)
        {
            remainingSql = null;
            ThrowIfDisposed();
            DbHandle.ThrowIfInvalid();

            if (sql.IsNullOrEmptyString())
                throw new ArgumentException(message: "Sql statement is empty", paramName: nameof(sql));

            InUseLock.Wait();
            try
            {
                // will throw on failure
                SQLitePCL.sqlite3_stmt stmt = PrepareStatement(sql, out remainingSql);
                return new WriteStatementContext(stmt, new SqlHash(sql, remainingSql), this);
            }
            finally
            {
                InUseLock.Release();
            }
        }

        /// <summary>Emit a 'BEGIN TRANSACTION' statement if one is not already open. Will automatically rollback if any error is encountered.</summary>
        /// <remarks>Caller should execute transaction statements in context of a wider connection lock wrapping the entire transaction.</remarks>
        public void BeginTransaction()
        {
            string sql = "BEGIN TRANSACTION";
            try
            {
                // sqlite does not allow nested transactions
                // (i.e. the 'BEGIN TRANS' syntax; it does allow nested SAVEPOINT transactions, but we are not supporting them yet)
                // therefore, only begin the transaction if one is not already started
                // 'autocommit' state is on (1) in default mode and no explicit transaction has been started
                // 'autocommit' state is disabled (0) with a BEGIN statement meaning an explicit transaction has been started
                if (SQLitePCL.raw.sqlite3_get_autocommit(base.DbHandle) > 0)
                    ExecuteTransactionVerb(sql);
            }
            catch
            {
                RollbackTransaction();
                throw;
            }
        }

        /// <summary>Emit a 'COMMIT TRANSACTION' statement if one is open. Will automatically rollback if any error is encountered.</summary>
        /// <remarks>Caller should execute transaction statements in context of a wider connection lock wrapping the entire transaction.</remarks>
        public void CommitTransaction()
        {
            string sql = "COMMIT TRANSACTION";
            try
            {
                // 'autocommit' state is on (1) in default mode and no explicit transaction has been started
                // 'autocommit' state is disabled (0) with a BEGIN statement meaning an explicit transaction has been started
                if (SQLitePCL.raw.sqlite3_get_autocommit(base.DbHandle) == 0)
                    ExecuteTransactionVerb(sql);
            }
            catch
            {
                RollbackTransaction();
                throw;
            }
        }

        /// <summary>Emite a 'ROLLBACK TRANSACTION' statement.</summary>
        /// <remarks>Caller should execute transaction statements in context of a wider connection lock wrapping the entire transaction.</remarks>
        public void RollbackTransaction()
        {
            string sql = "ROLLBACK TRANSACTION";
            // note: If the transaction has already been rolled back automatically by the error response, then the ROLLBACK command will fail with an error, but no harm is caused by this.
            try
            {
                // 'autocommit' state is on (1) in default mode and no explicit transaction has been started
                // 'autocommit' state is disabled (0) with a BEGIN statement meaning an explicit transaction has been started
                if (SQLitePCL.raw.sqlite3_get_autocommit(base.DbHandle) == 0)
                    ExecuteTransactionVerb(sql);
            }
            catch { /* if the rollback fails, the error is logged, but swallow it in favor of allowing the initial transaction exception (BEGIN OR COMMIT) to be thrown instead */ }
        }

        void ExecuteTransactionVerb(string sql)
        {
            ThrowIfDisposed();
            DbHandle.ThrowIfInvalid();

            int result = SQLitePCL.raw.sqlite3_exec(base.DbHandle, sql, out string errMsg);
            if ((result == SQLitePCL.raw.SQLITE_OK || result == SQLitePCL.raw.SQLITE_DONE) && errMsg.IsNullOrEmptyString())
                SqliteDatabase.LogQueue.CommandExecuted(sql);
            else
            {
                var transEx = new SqliteDbException(result: result, msg: $"{sql}: {result}-{ResultCodes.Lookup[result]} {errMsg}", filePath: base.FilePath);
                SqliteDatabase.LogQueue.CommandFailed(sql, transEx);
                throw new SqliteTransactionException(transEx, sql);
            }
        }

        /// <summary>
        /// Execute <paramref name="sql"/>. This method supports executing a multi-statement command text.
        /// Command text must have no parameters and any results are returned as a generic collection of names and values.
        /// This method is appropriate for DDL and PRAGMA commands that will not be cached and from which no meaningful row results are expected.
        /// Connection is internally locked for the duration of this operation.
        /// </summary>
        /// <param name="sql">The sql to execute. Can be a multi-statement command, but must have no parameters.</param>
        /// <param name="withCallback">
        /// True have any results returned as a generic collection of names and values. 
        /// False (default) to execute the sql without passing through a callback to collect any results.
        /// </param>
        public List<SqliteExecResult>? Execute(string sql, bool withCallback = false)
        {
            ThrowIfDisposed();
            DbHandle.ThrowIfInvalid();

            if (sql.IsNullOrEmptyString())
                return new List<SqliteExecResult>(0);

            List<SqliteExecResult>? resultCollector = withCallback ? SqliteExecResult.CreateCollector() : null;
            int result;
            string errMsg;
            StartTicks();

            try
            {
                InUseLock.Wait();
                try
                {
                    // execute sql
                    if (withCallback)
                        result = SQLitePCL.raw.sqlite3_exec(base.DbHandle, sql, SqliteExecResult.Callback, resultCollector, out errMsg);
                    else
                        result = SQLitePCL.raw.sqlite3_exec(base.DbHandle, sql, out errMsg);
                }
                finally
                {
                    InUseLock.Release();
                }

                // evaluate result
                if ((result == SQLitePCL.raw.SQLITE_OK || result == SQLitePCL.raw.SQLITE_DONE) && errMsg.IsNullOrEmptyString())
                {
                    SqliteDatabase.LogQueue.CommandExecuted(sql, GetElapsed());
                    return resultCollector;
                }
                else
                    throw new SqliteDbException(result: result, msg: $"From Execute: {result}-{ResultCodes.Lookup[result]} {errMsg}", filePath: base.FilePath);

            }
            catch (SqliteDbException ex)
            {
                SqliteDatabase.LogQueue.CommandFailed(sql, ex);
                throw;
            }
            catch (Exception ex)
            {
                // compose exception
                var cmdEx = new SqliteDbException(result: ResultCodes.NonSqliteException, msg: $"From Execute: {ex.Message}", filePath: base.FilePath, innerException: ex);
                // log failure
                SqliteDatabase.LogQueue.CommandFailed(sql, cmdEx);
                throw cmdEx;
            }
            finally
            {
                StopTicks();
            }
        }

        /// <summary>Clean up resources referenced by the <see cref="WriteConnectionContext"/>.</summary>
        protected override void Dispose(bool isManagedCall)
        {
            if (!_isAlreadyDisposed && isManagedCall)
            {
                // dispose of any prepared statement handles
                foreach (var kvp in _preparedStmtCache)
                {
                    try { kvp.Value?.Dispose(); } catch { }
                }
                _preparedStmtCache.Clear();
            }
            base.Dispose(isManagedCall);
        }
    }
}
