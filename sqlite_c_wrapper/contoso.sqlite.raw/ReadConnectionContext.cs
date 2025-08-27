using System;
using System.Collections.Generic;

namespace contoso.sqlite.raw
{
    /// <summary>
    /// Wraps a read-only opened sqlite database handle, any cached prepared statements created in context of the connection,
    /// and supports serializing access to the database so that the connection may be shared by one or more threads.
    /// </summary>
    public sealed class ReadConnectionContext : ConnectionContext
    {
        readonly Dictionary<SqlHash, ReadStatementContext> _preparedStmtCache;

        internal ReadConnectionContext(SQLitePCL.sqlite3 dbHandle, string filePath)
            : base(dbHandle, filePath)
        {
            _preparedStmtCache = new Dictionary<SqlHash, ReadStatementContext>();
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
        public new ReadStatementContext GetOrPrepare(string sql)
        {
            // preparation and use of a prepared statement handle must be a serial operation;
            // one connection, one statement at a time; a second prepare for the same sql would invalidate the first;
            InUseLock.Wait();
            try
            {
                var sqlHash = new SqlHash(sql);
                if (_preparedStmtCache.TryGetValue(sqlHash, out ReadStatementContext? ctx))
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
                ctx = new ReadStatementContext(stmt, new SqlHash(sql, remainingSql), this);
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
        public new ReadStatementContext Prepare(string sql) => Prepare(sql, out _);

        /// <summary>
        /// Invokes the native sqlite3_prepare_v2() method on <paramref name="sql"/> without caching the prepared statement.
        /// Also returns the full text of unparsed statements found after the first complete sql statement if <paramref name="sql"/>
        /// contains multiple statements.
        /// Caller should dispose of the context when finished executing the statement.
        /// </summary>
        public new ReadStatementContext Prepare(string sql, out string? remainingSql)
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
                return new ReadStatementContext(stmt, new SqlHash(sql, remainingSql), this);
            }
            finally
            {
                InUseLock.Release();
            }
        }

        /// <summary>
        /// Execute <paramref name="attachSql"/> for the read connection to attach another file or in-memory database.
        /// If an attach statement was already executed on the write connection with a shared cache, this statement should use the exact same syntax.
        /// </summary>
        /// <param name="attachSql">The 'ATTACH' sql to execute.</param>
        public void ExecuteAttach(string attachSql)
        {
            ThrowIfDisposed();
            DbHandle.ThrowIfInvalid();

            if (attachSql.IsNullOrEmptyString() || !attachSql.StartsWith("ATTACH", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Sql must be a non-empty 'ATTACH' statement");

            int result;
            string errMsg;
            StartTicks();

            try
            {
                InUseLock.Wait();
                try
                {
                    // execute sql
                    result = SQLitePCL.raw.sqlite3_exec(base.DbHandle, attachSql, out errMsg);
                }
                finally
                {
                    InUseLock.Release();
                }

                // evaluate result
                if ((result == SQLitePCL.raw.SQLITE_OK || result == SQLitePCL.raw.SQLITE_DONE) && errMsg.IsNullOrEmptyString())
                {
                    SqliteDatabase.LogQueue.CommandExecuted(attachSql, GetElapsed());
                    return;
                }
                else
                    throw new SqliteDbException(result: result, msg: $"From Execute: {result}-{ResultCodes.Lookup[result]} {errMsg}", filePath: base.FilePath);

            }
            catch (SqliteDbException ex)
            {
                SqliteDatabase.LogQueue.CommandFailed(attachSql, ex);
                throw;
            }
            catch (Exception ex)
            {
                // compose exception
                var cmdEx = new SqliteDbException(result: ResultCodes.NonSqliteException, msg: $"From Execute: {ex.Message}", filePath: base.FilePath, innerException: ex);
                // log failure
                SqliteDatabase.LogQueue.CommandFailed(attachSql, cmdEx);
                throw cmdEx;
            }
            finally
            {
                StopTicks();
            }
        }

        /// <summary>Clean up resources referenced by the <see cref="ReadConnectionContext"/>.</summary>
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
