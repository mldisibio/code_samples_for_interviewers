using System;
using System.Collections.Concurrent;
using System.IO;
using contoso.sqlite.raw.internals;

namespace contoso.sqlite.raw
{
    /// <summary>Wraps a single sqlite database file and provides a single point of entry to it.</summary>
    public class SqliteDatabase : IDisposable
    {
        // static fields
        // track db files already opened
        static readonly ConcurrentDictionary<string, SqliteDatabase> _openedFiles;
        // lock the static factory methods
        static readonly object _openFileLock;

        // note: SQLITE_OPEN_URI enables parsing of 'file:' or 'file:///' uri's; it is a newer feature so disabled by default, but is safe even if uri file names are not used

        // open read-only with shared cache and allow this api to manage connection sharing between threads
        const int _readOnlySharedOpenFlags = SQLitePCL.raw.SQLITE_OPEN_READONLY | SQLitePCL.raw.SQLITE_OPEN_URI | SQLitePCL.raw.SQLITE_OPEN_NOMUTEX | SQLitePCL.raw.SQLITE_OPEN_SHAREDCACHE;
        // open read-only with private cache and allow this api to manage only one connection for all threads
        const int _readOnlySingleOpenFlags = SQLitePCL.raw.SQLITE_OPEN_READONLY | SQLitePCL.raw.SQLITE_OPEN_URI | SQLitePCL.raw.SQLITE_OPEN_NOMUTEX | SQLitePCL.raw.SQLITE_OPEN_PRIVATECACHE;
        // open read-write with shared cache and allow this api to manage connection sharing between threads
        const int _readWriteSharedOpenFlags = SQLitePCL.raw.SQLITE_OPEN_READWRITE | SQLitePCL.raw.SQLITE_OPEN_URI | SQLitePCL.raw.SQLITE_OPEN_NOMUTEX | SQLitePCL.raw.SQLITE_OPEN_SHAREDCACHE;
        // open read-write with private cache and allow this api to manage only one connection for all threads
        const int _readWriteSingleOpenFlags = SQLitePCL.raw.SQLITE_OPEN_READWRITE | SQLitePCL.raw.SQLITE_OPEN_URI | SQLitePCL.raw.SQLITE_OPEN_NOMUTEX | SQLitePCL.raw.SQLITE_OPEN_PRIVATECACHE;
        // open read-write with shared cache and allow this api to manage connection sharing between threads, or create if not found
        const int _openOrCreateSharedOpenFlags = SQLitePCL.raw.SQLITE_OPEN_READWRITE | SQLitePCL.raw.SQLITE_OPEN_CREATE | SQLitePCL.raw.SQLITE_OPEN_URI | SQLitePCL.raw.SQLITE_OPEN_NOMUTEX | SQLitePCL.raw.SQLITE_OPEN_SHAREDCACHE;
        // open read-write with private cache and allow this api to manage one connection for all threads, or create if not found
        const int _openOrCreateSingleOpenFlags = SQLitePCL.raw.SQLITE_OPEN_READWRITE | SQLitePCL.raw.SQLITE_OPEN_CREATE | SQLitePCL.raw.SQLITE_OPEN_URI | SQLitePCL.raw.SQLITE_OPEN_NOMUTEX | SQLitePCL.raw.SQLITE_OPEN_PRIVATECACHE;
        // open in-memory with shared cache and allow this api to manage connection sharing between threads
        const int _writeMemorySharedOpenFlags = SQLitePCL.raw.SQLITE_OPEN_READWRITE | SQLitePCL.raw.SQLITE_OPEN_CREATE | SQLitePCL.raw.SQLITE_OPEN_URI | SQLitePCL.raw.SQLITE_OPEN_MEMORY | SQLitePCL.raw.SQLITE_OPEN_NOMUTEX | SQLitePCL.raw.SQLITE_OPEN_SHAREDCACHE;
        // open in-memory with shared cache and allow this api to manage connection sharing between threads
        const int _writeMemorySingleOpenFlags = SQLitePCL.raw.SQLITE_OPEN_READWRITE | SQLitePCL.raw.SQLITE_OPEN_CREATE | SQLitePCL.raw.SQLITE_OPEN_URI | SQLitePCL.raw.SQLITE_OPEN_MEMORY | SQLitePCL.raw.SQLITE_OPEN_NOMUTEX | SQLitePCL.raw.SQLITE_OPEN_PRIVATECACHE;

        // instance fields
        readonly string _filePath;
        readonly bool _readOnly;
        readonly bool _sharedCache;
        readonly bool _isMemory;
        // the SafeHandle wrapper around the pointer to the native sqlite instance, in this case a read-only db connection handle;
        // this handle will remain open for the lifetime of a 'SqliteDatabase' instance corresponding to a single sqlite file
        readonly ReadConnectionContext? _dbReadContext;
        // the SafeHandle wrapper around the pointer to the native sqlite instance, in this case a read/write db connection handle;
        // this handle will be created when first requested, but remain open for the remaining lifetime of the a 'SqliteDatabase' instance corresponding to a single sqlite file
        readonly WriteConnectionContext? _dbWriteContext;

        bool _isAlreadyDisposed;

        static SqliteDatabase()
        {
            _openFileLock = new object();
            LogQueue = ContextLog.Q;
            _openedFiles = new ConcurrentDictionary<string, SqliteDatabase>(concurrencyLevel: Environment.ProcessorCount, capacity: 64, comparer: StringComparer.Ordinal);
            // initializes the PCL infrastructure to use its own sqlite library (not one from the host platform)
            // this initialization only needs to be called once per process, and is not the same as opening a sqlite handle
            SQLitePCL.Batteries_V2.Init();
        }

        // requesting 'shared cache' means requesting access for multiple connections,
        //   which this library limits (by choice) to two connections only, one write and one read;
        // 'private cache' means requesting one connection only for all access to the current database;
        // in both cases, this api manages concurrency such that any connection can only be used by one thread at a time
        //   even if sqlite is more relaxed, we are doing it to minimize contention for maximum throughput by treating each database like we would treat file I/O;
        // in addition to read-write flags, we also control read write by only allowing reader (R in CRUD) commands to the read connection
        SqliteDatabase(string filePath, int openFlags, string? pragmas = null)
        {
            _filePath = filePath;
            _readOnly = (openFlags & SQLitePCL.raw.SQLITE_OPEN_READONLY) == SQLitePCL.raw.SQLITE_OPEN_READONLY;
            _sharedCache = (openFlags & SQLitePCL.raw.SQLITE_OPEN_SHAREDCACHE) == SQLitePCL.raw.SQLITE_OPEN_SHAREDCACHE;

            if (!_readOnly)
            {
                // request is for a write-enabled disk or memory database, so we'll need a write-enabled connection no matter what
                // open the read-write handle with the given flags, which will have the ReadWrite flag no matter what, and will have the Memory flag if in-memory
                SQLitePCL.sqlite3 writeHandle = OpenDatabaseHandle(openFlags);
                _dbWriteContext = new WriteConnectionContext(writeHandle, _filePath);
                // execute pragmas
                if (pragmas.IsNotNullOrEmptyString())
                    ExecutePragmas(pragmas, _dbWriteContext);
            }

            _isMemory = (openFlags & SQLitePCL.raw.SQLITE_OPEN_MEMORY) == SQLitePCL.raw.SQLITE_OPEN_MEMORY;
            if (_isMemory)
            {
                // request is for an in-memory database;
                // if a shared cached is requested, then open the second 'read-only' connection;
                if (_sharedCache)
                {
                    // this needs to be opened with the write flag enabled, but we'll wrap it with a read connection object, which limits user commands to readers
                    // (at least so far testing shows this 'might' be necessary)
                    SQLitePCL.sqlite3 readHandle = OpenDatabaseHandle(_writeMemorySharedOpenFlags);
                    _dbReadContext = new ReadConnectionContext(readHandle, _filePath);
                }
            }
            else
            {
                // request is for a write or for a read-only database, but not in-memory;
                // if write and a shared cached is requested, then open the second 'read-only' managed connection;
                // if read-only, we're opening the first (and only) read-only connnection
                if (_readOnly || _sharedCache)
                {
                    SQLitePCL.sqlite3 readHandle = _sharedCache ? OpenDatabaseHandle(_readOnlySharedOpenFlags) : OpenDatabaseHandle(openFlags);
                    _dbReadContext = new ReadConnectionContext(readHandle, _filePath);
                }
            }
        }

        static void ExecutePragmas(string pragmas, WriteConnectionContext ctx)
        {
            if (pragmas.IsNotNullOrEmptyString())
            {
                using var writeLock = ctx.Lock();
                ctx.Execute(pragmas);
            }
        }

        /// <summary>One event stream handler for the application domain lifetime.</summary>
        internal static ContextLog LogQueue { get; }

        /// <summary>Set an <see cref="ISqliteLogger"/> log implementation once per application domain.</summary>
        public static void SetEventLogger(ISqliteLogger logger)
        {
            if (logger != null)
                LogQueue.SetListener(logger);
        }

        /// <summary>Set an <see cref="ISqliteLogger"/> log implementation once per application domain.</summary>
        public static void CloseEventLogger()
        {
            try { LogQueue.Dispose(); }
            catch { }
        }

        /// <summary>Provide connection history to verify handles are property closed.</summary>
        public static string GetConnectionHistory(out int opened, out int closed)
        {
            return LogQueue.GetConnectionHistory(out opened, out closed);
        }

        /// <summary>
        /// Initializes a read-only instance of the <see cref="SqliteDatabase"/> class over <paramref name="filePath"/>.
        /// File must already exist. Note: this instance supports only one read-only connection, with a private cache.
        /// </summary>
        /// <param name="filePath">Full path of the database to open. File must already exist.</param>
        public static SqliteDatabase OpenReadOnly(string filePath)
        {
            lock (_openFileLock)
            {
                string fullPath = Path.GetFullPath(filePath);
                // for read-only, file must already exist
                if (!File.Exists(fullPath))
                    throw new FileNotFoundException(message: "Use 'OpenOrCreate' if file does not yet exist.", fileName: fullPath);

                // see if path already opened and in read-only mode
                if (_openedFiles.TryGetValue(fullPath, out SqliteDatabase? db))
                {
                    if (db._readOnly)
                        return db;
                    else
                        // this library want to manage concurrency by returning any handle already opened
                        // but only if in a compatible read-only mode
                        throw new InvalidOperationException($"File already opened for write access [{fullPath}]");
                }

                // not opened yet, so create
                var readOnlyDb = new SqliteDatabase(fullPath, _readOnlySingleOpenFlags);
                // ensure no race condition opened the same path in the meantime
                if (_openedFiles.TryAdd(fullPath, readOnlyDb))
                    return readOnlyDb;
                else
                {
                    readOnlyDb.Dispose();
                    throw new InvalidOperationException($"File opened by another thread (possible race condition) [{fullPath}]");
                }
            }
        }

        /// <summary>
        /// Initializes a read-write instance of the <see cref="SqliteDatabase"/> class over <paramref name="filePath"/>.
        /// File must already exist.
        /// </summary>
        /// <param name="filePath">Path to the sqlite database on disk. File must already exist.</param>
        /// <param name="pragmas">An optional string containing one or more PRAGMA statements by which to modify sqlite defaults.</param>
        /// <param name="sharedReadWrite">
        /// If true, makes two connections available, one read-only, one write, sharing a common cache, and allowing concurrency of the two independent connections.
        /// If false (default), only one write-enabled connection is available with a private cache.
        /// In both cases, operations are serialized on each connection itself.
        /// </param>
        public static SqliteDatabase OpenReadWrite(string filePath, string? pragmas = null, bool sharedReadWrite = false)
        {
            lock (_openFileLock)
            {
                string fullPath = Path.GetFullPath(filePath);
                // for read-write, file must already exist
                if (!File.Exists(fullPath))
                    throw new FileNotFoundException(message: "Use 'OpenOrCreate' if file does not yet exist.", fileName: fullPath);

                // see if path already opened and in read-write mode
                if (_openedFiles.TryGetValue(fullPath, out SqliteDatabase? db))
                {
                    if (!db._readOnly)
                        return db;
                    else
                        // this library want to manage concurrency by returning any handle already opened
                        // but only if in a compatible read-write mode
                        throw new InvalidOperationException($"File already opened for read-only access [{fullPath}]");
                }

                // not opened yet, so create
                int openFlags = sharedReadWrite ? _readWriteSharedOpenFlags : _readWriteSingleOpenFlags;
                var readWriteDb = new SqliteDatabase(fullPath, openFlags, pragmas);
                // ensure no race condition opened the same path in the meantime
                if (_openedFiles.TryAdd(fullPath, readWriteDb))
                    return readWriteDb;
                else
                {
                    readWriteDb.Dispose();
                    throw new InvalidOperationException($"File opened by another thread (possible race condition) [{fullPath}]");
                }
            }
        }

        /// <summary>
        /// Initializes a read-write instance of the <see cref="SqliteDatabase"/> class over <paramref name="filePath"/>.
        /// File will be created if it does not already exist.
        /// </summary>
        /// <param name="filePath">Path to the sqlite database on disk or where it should be written to if not yet created.</param>
        /// <param name="pragmas">An optional set of one or more PRAGMA statements by which to modify sqlite defaults.</param>
        /// <param name="sharedReadWrite">
        /// If true, makes two connections available, one read-only, one write, sharing a common cache, and allowing concurrency of the two independent connections.
        /// If false (default), only one write-enabled connection is available with a private cache.
        /// In both cases, operations are serialized on each connection itself.
        /// </param>
        public static SqliteDatabase OpenOrCreate(string filePath, string? pragmas = null, bool sharedReadWrite = false)
        {
            lock (_openFileLock)
            {
                string fullPath = Path.GetFullPath(filePath);

                // see if path already opened and in read-write mode
                if (_openedFiles.TryGetValue(fullPath, out SqliteDatabase? db))
                {
                    if (!db._readOnly)
                        return db;
                    else
                        // this library want to manage concurrency by returning any handle already opened
                        // but only if in a compatible read-write-create mode
                        throw new InvalidOperationException($"File already opened for read-only access [{fullPath}]");
                }

                // not opened yet, so create
                int openFlags = sharedReadWrite ? _openOrCreateSharedOpenFlags : _openOrCreateSingleOpenFlags;
                var readWriteDb = new SqliteDatabase(fullPath, openFlags, pragmas);
                // ensure no race condition opened the same path in the meantime
                if (_openedFiles.TryAdd(fullPath, readWriteDb))
                    return readWriteDb;
                else
                {
                    readWriteDb.Dispose();
                    throw new InvalidOperationException($"File opened by another thread (possible race condition) [{fullPath}]");
                }
            }
        }

        /// <summary>
        /// Initializes an in-memory instance of the <see cref="SqliteDatabase"/> class with a single write-enabled connection, and private cache
        /// </summary>
        /// <param name="pragmas">An optional set of one or more PRAGMA statements by which to modify sqlite defaults.</param>
        public static SqliteDatabase OpenInMemory(string? pragmas = null)
        {
            // in-memory, private cache:                     ":memory:" or "file::memory:";          ATTACH DATABASE 'file::memory:' AS aux1;
            // shared-cache, one db for entire process:      "file::memory:?cache=shared";           ATTACH DATABASE 'file::memory:?cache=shared' AS aux1;
            // multiple in-memory, shared, for same process: "file:memdb1?mode=memory&cache=shared"; ATTACH DATABASE 'file:memdb1?mode=memory&cache=shared' AS aux1;
            lock (_openFileLock)
            {
                string memUri = $"file::memory:";
                //string cacheUri = $"file::memory:";
                return new SqliteDatabase(memUri, _writeMemorySingleOpenFlags, pragmas);
            }
            // from https://sqlite.org/inmemorydb.html:
            // Every :memory: database is distinct from every other. So, opening two database connections each with the filename ":memory:" will create two independent in-memory databases.
            // If the unadorned ":memory:" name is used to specify the in-memory database, then that database always has a private cache
            //   and is this only visible to the database connection that originally opened it.
            // However, the same in-memory database can be opened by two or more database using the shared-cache syntax: "file::memory:?cache=shared";
            //   (assumes only one in-memory database for the current process)
            // If two or more distinct but shareable in-memory databases are needed in a single process, use the named uri syntax: "file:memdb1?mode=memory&cache=shared"
        }

        /// <summary>
        /// Initializes an in-memory instance of the <see cref="SqliteDatabase"/> class with two connections available, one read-only, one write, 
        /// sharing a common cache, and allowing concurrency of the two independent connections. Operations are serialized on each connection itself.
        /// </summary>
        /// <param name="sharedCacheName">The name of the shared cache of the same in-memory database allowing it to be shared by multiple threads when accessed by name.</param>
        /// <param name="pragmas">An optional set of one or more PRAGMA statements by which to modify sqlite defaults.</param>
        public static SqliteDatabase OpenInMemoryShared(string sharedCacheName, string? pragmas = null)
        {
            // in-memory, private cache:                     ":memory:" or "file::memory:";          ATTACH DATABASE 'file::memory:' AS aux1;
            // shared-cache, one db for entire process:      "file::memory:?cache=shared";           ATTACH DATABASE 'file::memory:?cache=shared' AS aux1;
            // multiple in-memory, shared, for same process: "file:memdb1?mode=memory&cache=shared"; ATTACH DATABASE 'file:memdb1?mode=memory&cache=shared' AS aux1;
            lock (_openFileLock)
            {
                string cacheName = sharedCacheName.IsNullOrEmptyString() ? Path.GetRandomFileName() : sharedCacheName;
                string cacheUri = $"file:{cacheName}?mode=memory&cache=shared";
                //string cacheUri = $"file::memory:";
                return new SqliteDatabase(cacheUri, _writeMemorySharedOpenFlags, pragmas);
            }
        }

        /// <summary>
        /// Initializes a read-write instance of the <see cref="SqliteDatabase"/> class over <paramref name="filePath"/> as a backup target.
        /// File will be overwritten if it already exists.
        /// </summary>
        /// <param name="filePath">Path to where the sqlite backup should be written.</param>
        internal static SqliteDatabase OpenForBackup(string filePath)
        {
            lock (_openFileLock)
            {
                try
                {
                    string fullPath = Path.GetFullPath(filePath);

                    // overwrite any existing backup file
                    if (File.Exists(fullPath))
                        File.Delete(fullPath);

                    // open a read-write handle
                    var readWriteDb = new SqliteDatabase(fullPath, _openOrCreateSingleOpenFlags);

                    // close the read-only connection; we only want one open connection for backup
                    // not needed with new 'Single' options
                    // readWriteDb._dbReadContext.Dispose();

                    // return handle
                    return readWriteDb;
                }
                catch (SqliteDbException) { throw; }
                catch (Exception ex)
                {
                    // compose exception
                    var openEx = new SqliteDbException(result: ResultCodes.NonSqliteException, msg: $"From OpenForBackup: {ex.Message}", filePath: filePath, innerException: ex);
                    // log failure
                    SqliteDatabase.LogQueue.ConnectionFailed(filePath, openEx);
                    throw openEx;
                }
            }
        }

        SQLitePCL.sqlite3 OpenDatabaseHandle(int openFlags)
        {
            string? vfs = null;
            int result = SQLitePCL.raw.sqlite3_open_v2(_filePath, out SQLitePCL.sqlite3 dbHandle, openFlags, vfs!);
            if (result == SQLitePCL.raw.SQLITE_OK)
            {
                LogQueue.ConnectionOpened();
                return dbHandle;
            }
            else
            {
                string errMsg = TryRetrieveError(dbHandle, result);
                TryCloseHandle(dbHandle);
                // not being able to open the database is treated here as a fatal error
                var openEx = new SqliteDbException(result: result, msg: errMsg, filePath: this._filePath);
                LogQueue.ConnectionFailed(_filePath, openEx);
                throw openEx;
            }
        }

        /// <summary>Obtain access to the shared read-only connection.</summary>
        public ReadConnectionContext GetOpenedReadContext() => 
            (_readOnly || _sharedCache) ? _dbReadContext! : throw new InvalidOperationException($"{Path.GetFileName(_filePath)} is opened with one (write-enabled) connection only");

        /// <summary>Obtain access to the shared read-write connection if the database was opened with write permission.</summary>
        public WriteConnectionContext GetOpenedWriteContext() => 
            _readOnly ? throw new InvalidOperationException($"{Path.GetFileName(_filePath)} is opened in read-only mode") : _dbWriteContext!;

        /// <summary>
        /// Obtain a read-only connection that must be managed and disposed by the caller. Connection is not cached nor maintained by the api.
        /// Concurrency must be managed by the user. Intended use-case is for short-lived, ephemeral read connections when the existing managed connections
        /// are all in use.
        /// </summary>
        /// <param name="withSharedCache">
        /// True to open in shared-cache mode. For read-only connections, there is less lock contention if cache mode is left private (false),
        /// at the expense of more memory consumption. 
        /// </param>
        public ReadConnectionContext GetDisposableReadOnlyContext(bool withSharedCache = false)
        {
            // see http://sqlite.1065341.n5.nabble.com/Fastest-concurrent-read-only-performance-threads-vs-processes-td34363.html
            // for remarks on how sqlite3_step() will block in shared-cache mode

            // if requesting a new connection to an in-memory database, that database must have been opened with a shared cache;
            // the subsequent connections may opt for a private cache; but they will not see any data if the memory db was created with a private cache;
            if (_isMemory && !_sharedCache)
                throw new InvalidOperationException("In-Memory must have been created with shared cache for additional connections to be granted.");

            // (memory database must be connected to with a write connection, but can be wrapped in a read-only api)
            int openFlags = withSharedCache
                          ? _isMemory ? _writeMemorySharedOpenFlags : _readOnlySharedOpenFlags
                          : _isMemory ? _writeMemorySingleOpenFlags : _readOnlySingleOpenFlags;

            SQLitePCL.sqlite3 readHandle = OpenDatabaseHandle(openFlags);
            return new ReadConnectionContext(readHandle, _filePath);
        }


        internal static string TryRetrieveError(SQLitePCL.sqlite3 dbHandle, int? resultCode)
        {
            string? errMsg = null;
            string? rcText = null;
            try
            {
                if (dbHandle.SeemsValid())
                {
                    errMsg = SQLitePCL.raw.sqlite3_errmsg(dbHandle).utf8_to_string();
                    if (!resultCode.HasValue)
                        resultCode = SQLitePCL.raw.sqlite3_errcode(dbHandle);
                }
            }
            catch { }

            if (resultCode.HasValue)
                rcText = $"{resultCode}-{ResultCodes.Lookup[resultCode.Value]}";

            bool noMsg = errMsg.IsNullOrEmptyString();
            bool noRC = rcText.IsNullOrEmptyString();
            if (noMsg && noRC)
                return "Undefined error";
            string sp = noMsg || noRC ? string.Empty : " ";
            return $"{rcText}{sp}{errMsg}";
        }

        internal static void TryCloseHandle(SQLitePCL.sqlite3 dbHandle)
        {
            try
            {
                if (dbHandle.SeemsValid())
                {
                    int result = SQLitePCL.raw.sqlite3_close_v2(dbHandle);
                    if (result == SQLitePCL.raw.SQLITE_OK)
                        LogQueue.ConnectionClosed();
                    else
                    {
                        var closeEx = new SqliteDbException(result: result, msg: $"{result}-{ResultCodes.Lookup[result]}");
                        LogQueue.TraceError(closeEx, "Attempting to close database connection");
                    }
                }
            }
            catch { }
        }

        #region IDisposable

        void Dispose(bool isManagedCall)
        {
            if (!_isAlreadyDisposed)
            {
                _isAlreadyDisposed = true;

                if (isManagedCall)
                {
                    _openedFiles.TryRemove(_filePath, out _);
                    // finalize any prepared statements cached by each collection
                    // finalize any open connection handles
                    try { _dbWriteContext?.Dispose(); } catch { }
                    try { _dbReadContext?.Dispose(); } catch { }
                }
            }
        }

        /// <summary>Close the handles (and other resources) to the database file managed by this <see cref="SqliteDatabase"/> instance.</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
