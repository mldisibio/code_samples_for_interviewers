using System.Diagnostics;

namespace contoso.sqlite.raw
{
    /// <summary>
    /// Wraps an opened sqlite database handle, any cached prepared statements created in context of the connection,
    /// and supports serializing access to the database so that the connection may be shared by one or more threads.
    /// </summary>
    public abstract class ConnectionContext : SqliteContext
    {
        // lock (to be encapsulated as IDisposable) for the caller to wrap one or more connection operations
        // and to support both synchronous and asynchronous locking
        readonly SemaphoreSlim _userLock;
        readonly IDisposable _userLockReleaser;
        readonly Task<IDisposable> _userLockAvailableTask;
        readonly Stopwatch _sw;

        /// <summary>Initialize with the sqlite handle and path to the database file.</summary>
        protected ConnectionContext(SQLitePCL.sqlite3 dbHandle, string filePath)
            : base(dbHandle, filePath)
        {
            InUseLock = new SemaphoreSlim(1, 1);
            _sw = new Stopwatch();

            _userLock = new SemaphoreSlim(1, 1);
            _userLockReleaser = new ContextLock(_userLock);
            _userLockAvailableTask = Task.FromResult(_userLockReleaser);
        }

        /// <summary>Internal lock to ensure of all uses of this connection instance are serialized</summary>
        internal SemaphoreSlim InUseLock { get; }

        /// <summary>
        /// Invokes the native sqlite3_prepare_v2() method on <paramref name="sql"/> without caching the prepared statement.
        /// Caller should dispose of the context when finished executing the statement.
        /// </summary>
        /// <remarks>
        /// Note that sqlite3_prepare_v2() only parses the first complete sql statement 
        /// (i.e. parsing and execution of multiple sql statements in one string is not supported without some sort of iteration.
        /// </remarks>
        public StatementContext Prepare(string sql) => Prepare(sql, out _);

        /// <summary>
        /// Invokes the native sqlite3_prepare_v2() method on <paramref name="sql"/> without caching the prepared statement.
        /// Also returns the full text of unparsed statements found after the first complete sql statement if <paramref name="sql"/>
        /// contains multiple statements.
        /// Caller should dispose of the context when finished executing the statement.
        /// </summary>
        public StatementContext Prepare(string sql, out string? remainingSql) => PrepareCore(sql, out remainingSql);

        /// <summary>
        /// Retrieves the already prepared statement for <paramref name="sql"/> from cache
        /// or invokes the native sqlite3_prepare_v2() method on <paramref name="sql"/>, caching the result for later reuse.
        /// </summary>
        /// <remarks>
        /// Note that sqlite3_prepare_v2() only parses the first complete sql statement 
        /// (i.e. parsing and execution of multiple sql statements in one string is not supported without some sort of iteration.
        /// </remarks>
        public StatementContext GetOrPrepare(string sql) => GetOrPrepareCore(sql);

        /// <summary>
        /// Invoke the native sqlite3_prepare_v2() method on <paramref name="sql"/> without caching the prepared statement.
        /// Returns the full text of unparsed statements found after the first complete sql statement if <paramref name="sql"/>
        /// contains multiple statements.
        /// </summary>
        protected abstract StatementContext PrepareCore(string sql, out string? remainingSql);

        /// <summary>
        /// Retrieve the already prepared statement for <paramref name="sql"/> from cache
        /// or invoke the native sqlite3_prepare_v2() method on <paramref name="sql"/>, caching the result for later reuse.
        /// </summary>
        protected abstract StatementContext GetOrPrepareCore(string sql);

        /// <summary>
        /// Invokes the native sqlite3_prepare_v2() method on <paramref name="sql"/> without caching the prepared statement.
        /// Also returns the full text of unparsed statements found after the first complete sql statement if <paramref name="sql"/>
        /// contains multiple statements.
        /// </summary>
        protected SQLitePCL.sqlite3_stmt PrepareStatement(string sql, out string? remainingSql)
        {
            int result = SQLitePCL.raw.sqlite3_prepare_v2(DbHandle, sql, out SQLitePCL.sqlite3_stmt stmt, out remainingSql);
            if (result == SQLitePCL.raw.SQLITE_OK)
            {
                return stmt;
            }
            else
            {
                string errMsg = $"{SqliteDatabase.TryRetrieveError(DbHandle, result)} from 'sqlite3_prepare_v2'";
                TryFinalizeStatement(stmt);
                var prepEx = new SqliteDbException(result: result, msg: errMsg, filePath: this.FilePath);
                SqliteDatabase.LogQueue.CommandFailed(sql, prepEx);
                throw prepEx;
            }
        }

        /// <summary>Usually returns the rowid of the most recent successful INSERT into a rowid table completed on the current connection.</summary>
        public long LastInsertRowId()
        {
            ThrowIfDisposed();
            DbHandle.ThrowIfInvalid();
            // unlocked read-only operation
            return SQLitePCL.raw.sqlite3_last_insert_rowid(DbHandle);
        }

        /// <summary>Returns the number of rows affected by the most recently completed INSERT, UPDATE or DELETE statement on the current connection.</summary>
        public int RowsAffected()
        {
            ThrowIfDisposed();
            DbHandle.ThrowIfInvalid();
            // unlocked read-only operation
            return SQLitePCL.raw.sqlite3_changes(DbHandle);
        }

        /// <summary>Returns the total number of rows affected by all INSERT, UPDATE or DELETE statements completed since the current connection was opened.</summary>
        public int CummulativeRowsAffected()
        {
            ThrowIfDisposed();
            DbHandle.ThrowIfInvalid();
            // unlocked read-only operation
            return SQLitePCL.raw.sqlite3_total_changes(DbHandle);
        }

        /// <summary>
        /// Return the database (or databases if others were attached), tables, views, and columns available to the current database connection.
        /// This schema is read each time this method is invoked. Client can cache the result, but is responsible for refreshing the contents.
        /// </summary>
        public SchemaContext ReadSchema() => SchemaContext.CreateFrom(this);

        /// <summary>
        /// Backup the current database instance to a destination file on disk using the current database connection.
        /// This operation internally locks both source and destination connections while executing.
        /// </summary>
        public BackupContextFrom Backup() => new BackupContextFrom(this);

        /// <summary>Low-level api requiring use of SQLitePCL.raw to register a user-defined function.</summary>
        public RegisterFunctionContext Register() => new RegisterFunctionContext(this);

        /// <summary>
        /// Lock the sqlite connection handle for the duration of one or more synchronous operations,
        /// effectively serializing access to the database file.
        /// Call 'Dispose' to release the lock. Nested locking is not supported.
        /// </summary>
        /// <param name="millisecondsTimeout">
        /// Number of milliseconds to wait.
        /// If specified with zero or positive integer, and lock is not obtained within specified timeout, a <see cref="TimeoutException"/> is thrown.
        /// If -1 or not specified, will wait indefinitely.
        /// </param>
        /// <param name="cancelToken">The <see cref="CancellationToken"/> to observe.</param>
        public IDisposable Lock(int millisecondsTimeout = -1, CancellationToken cancelToken = default)
        {
            // check that our ConnectionContext itself has not been disposed already
            ThrowIfDisposed();
            if (millisecondsTimeout > -1)
            {
                // note that zero is usually meant to test the lock, but because of our constructs
                // we'll throw a timeout exception if the lock is contended
                bool obtained = _userLock.Wait(millisecondsTimeout, cancelToken);
                if (!obtained)
                    throw new TimeoutException(message: $"Connection lock not obtained after {millisecondsTimeout} ms");
            }
            else
            {
                _userLock.Wait(cancelToken);
            }
            // if obtained, with or without a timeout constraint, return the lock releaser
            return _userLockReleaser;
        }

        /// <summary>
        /// Lock the sqlite connection handle for the duration of one or more asynchronous operations,
        /// effectively serializing access to the database file.
        /// Call 'Dispose' to release the lock. Nested locking is not supported.
        /// </summary>
        /// <param name="cancelToken">The <see cref="CancellationToken"/> to observe.</param>
        public Task<IDisposable> LockAsync(CancellationToken cancelToken = default)
        {
            // check that our ConnectionContext itself has not been disposed already
            ThrowIfDisposed();

            Task waitForLockTask = _userLock.WaitAsync(cancelToken);
            // if the lock is not contended, return the cached task that wraps the IDisposable releaser so as not to create a new task;
            // if contended, return the wait task configured with a continuation task that returns the (cached) IDisposable releaser
            return waitForLockTask.IsCompleted
                   ? _userLockAvailableTask
                   : waitForLockTask.ContinueWith((_, state) => (IDisposable)state!,
                                                  _userLockReleaser,
                                                  cancelToken,
                                                  TaskContinuationOptions.ExecuteSynchronously,
                                                  TaskScheduler.Default);
        }

        /// <summary>Finalize a sqlite prepared statement handle.</summary>
        public static void TryFinalizeStatement(SQLitePCL.sqlite3_stmt stmt)
        {
            if (stmt.SeemsValid())
            {
                try { SQLitePCL.raw.sqlite3_finalize(stmt); }
                catch { }
            }
        }

        /// <summary>Start a sql operation performance timer.</summary>
        internal void StartTicks()
        {
            StopTicks();
            _sw.Start();
        }

        /// <summary>Ensure the performance timer is stopped, e.g. if operation threw an exception.</summary>
        internal void StopTicks()
        {
            if (_sw.IsRunning)
            {
                _sw.Stop();
                _sw.Reset();
            }
        }

        /// <summary>Stop the performance timer and return the milliseconds elapsed.</summary>
        internal long? GetElapsed()
        {
            if (_sw.IsRunning)
            {
                _sw.Stop();
                long elapsed = _sw.ElapsedMilliseconds;
                _sw.Reset();
                return elapsed;
            }
            return default;
        }

        /// <summary>Clean up resources referenced by the <see cref="ConnectionContext"/>.</summary>
        protected override void Dispose(bool isManagedCall)
        {
            if (!_isAlreadyDisposed && isManagedCall)
            {
                _sw.Stop();
                InUseLock.Dispose();
                _userLock.Dispose();
                // dispose the 'connection' handle
                SqliteDatabase.TryCloseHandle(DbHandle);
            }
            base.Dispose(isManagedCall);
        }


        /// <summary>
        /// This is an <see cref="IDisposable"/> whose purpose is to allow the caller to serialize use of this one connection  
        /// to a sqlite database across one or more operations by taking a lock and releasing it on disposal.
        /// </summary>
        /// <remarks>
        /// see https://stackoverflow.com/a/21011273/458354 and https://devblogs.microsoft.com/pfxteam/building-async-coordination-primitives-part-6-asynclock/
        /// for explanation of this construct and pattern
        /// </remarks>
        readonly struct ContextLock : IDisposable
        {
            readonly SemaphoreSlim _lockObj;

            public ContextLock(SemaphoreSlim lockObj) => _lockObj = lockObj;

            public void Dispose() => _lockObj.Release();
        }
    }
}
