using System;
using System.Collections.Generic;

namespace contoso.sqlite.raw
{

    /// <summary>Wraps sqlite result codes as named values.</summary>
    public class ResultCodes
    {
        static readonly Dictionary<int, string> _lookup;
        static readonly Lazy<ResultCodes> _instance = new Lazy<ResultCodes>(() => new ResultCodes());

        ResultCodes() { }

        static ResultCodes()
        {
            _lookup = new Dictionary<int, string>
            {
                [SQLitePCL.raw.SQLITE_OK] = "SQLITE_OK",
                [SQLitePCL.raw.SQLITE_ERROR] = "SQLITE_ERROR",
                [SQLitePCL.raw.SQLITE_INTERNAL] = "SQLITE_INTERNAL",
                [SQLitePCL.raw.SQLITE_PERM] = "SQLITE_PERM",
                [SQLitePCL.raw.SQLITE_ABORT] = "SQLITE_ABORT",
                [SQLitePCL.raw.SQLITE_BUSY] = "SQLITE_BUSY",
                [SQLitePCL.raw.SQLITE_LOCKED] = "SQLITE_LOCKED",
                [SQLitePCL.raw.SQLITE_NOMEM] = "SQLITE_NOMEM",
                [SQLitePCL.raw.SQLITE_READONLY] = "SQLITE_READONLY",
                [SQLitePCL.raw.SQLITE_INTERRUPT] = "SQLITE_INTERRUPT",
                [SQLitePCL.raw.SQLITE_IOERR] = "SQLITE_IOERR",
                [SQLitePCL.raw.SQLITE_CORRUPT] = "SQLITE_CORRUPT",
                [SQLitePCL.raw.SQLITE_NOTFOUND] = "SQLITE_NOTFOUND",
                [SQLitePCL.raw.SQLITE_FULL] = "SQLITE_FULL",
                [SQLitePCL.raw.SQLITE_CANTOPEN] = "SQLITE_CANTOPEN",
                [SQLitePCL.raw.SQLITE_PROTOCOL] = "SQLITE_PROTOCOL",
                [SQLitePCL.raw.SQLITE_EMPTY] = "SQLITE_EMPTY",
                [SQLitePCL.raw.SQLITE_SCHEMA] = "SQLITE_SCHEMA",
                [SQLitePCL.raw.SQLITE_TOOBIG] = "SQLITE_TOOBIG",
                [SQLitePCL.raw.SQLITE_CONSTRAINT] = "SQLITE_CONSTRAINT",
                [SQLitePCL.raw.SQLITE_MISMATCH] = "SQLITE_MISMATCH",
                [SQLitePCL.raw.SQLITE_MISUSE] = "SQLITE_MISUSE",
                [SQLitePCL.raw.SQLITE_NOLFS] = "SQLITE_NOLFS",
                [SQLitePCL.raw.SQLITE_AUTH] = "SQLITE_AUTH",
                [SQLitePCL.raw.SQLITE_FORMAT] = "SQLITE_FORMAT",
                [SQLitePCL.raw.SQLITE_RANGE] = "SQLITE_RANGE",
                [SQLitePCL.raw.SQLITE_NOTADB] = "SQLITE_NOTADB",
                [SQLitePCL.raw.SQLITE_NOTICE] = "SQLITE_NOTICE",
                [SQLitePCL.raw.SQLITE_WARNING] = "SQLITE_WARNING",
                [SQLitePCL.raw.SQLITE_ROW] = "SQLITE_ROW",
                [SQLitePCL.raw.SQLITE_DONE] = "SQLITE_DONE",
                [(SQLitePCL.raw.SQLITE_ERROR | (1 << 8))] = "SQLITE_ERROR_MISSING_COLLSEQ",
                [(SQLitePCL.raw.SQLITE_ERROR | (2 << 8))] = "SQLITE_ERROR_RETRY",
                [(SQLitePCL.raw.SQLITE_ERROR | (3 << 8))] = "SQLITE_ERROR_SNAPSHOT",
                [(SQLitePCL.raw.SQLITE_IOERR | (1 << 8))] = "SQLITE_IOERR_READ",
                [(SQLitePCL.raw.SQLITE_IOERR | (2 << 8))] = "SQLITE_IOERR_SHORT_READ",
                [(SQLitePCL.raw.SQLITE_IOERR | (3 << 8))] = "SQLITE_IOERR_WRITE",
                [(SQLitePCL.raw.SQLITE_IOERR | (4 << 8))] = "SQLITE_IOERR_FSYNC",
                [(SQLitePCL.raw.SQLITE_IOERR | (5 << 8))] = "SQLITE_IOERR_DIR_FSYNC",
                [(SQLitePCL.raw.SQLITE_IOERR | (6 << 8))] = "SQLITE_IOERR_TRUNCATE",
                [(SQLitePCL.raw.SQLITE_IOERR | (7 << 8))] = "SQLITE_IOERR_FSTAT",
                [(SQLitePCL.raw.SQLITE_IOERR | (8 << 8))] = "SQLITE_IOERR_UNLOCK",
                [(SQLitePCL.raw.SQLITE_IOERR | (9 << 8))] = "SQLITE_IOERR_RDLOCK",
                [(SQLitePCL.raw.SQLITE_IOERR | (10 << 8))] = "SQLITE_IOERR_DELETE",
                [(SQLitePCL.raw.SQLITE_IOERR | (11 << 8))] = "SQLITE_IOERR_BLOCKED",
                [(SQLitePCL.raw.SQLITE_IOERR | (12 << 8))] = "SQLITE_IOERR_NOMEM",
                [(SQLitePCL.raw.SQLITE_IOERR | (13 << 8))] = "SQLITE_IOERR_ACCESS",
                [(SQLitePCL.raw.SQLITE_IOERR | (14 << 8))] = "SQLITE_IOERR_CHECKRESERVEDLOCK",
                [(SQLitePCL.raw.SQLITE_IOERR | (15 << 8))] = "SQLITE_IOERR_LOCK",
                [(SQLitePCL.raw.SQLITE_IOERR | (16 << 8))] = "SQLITE_IOERR_CLOSE",
                [(SQLitePCL.raw.SQLITE_IOERR | (17 << 8))] = "SQLITE_IOERR_DIR_CLOSE",
                [(SQLitePCL.raw.SQLITE_IOERR | (18 << 8))] = "SQLITE_IOERR_SHMOPEN",
                [(SQLitePCL.raw.SQLITE_IOERR | (19 << 8))] = "SQLITE_IOERR_SHMSIZE",
                [(SQLitePCL.raw.SQLITE_IOERR | (20 << 8))] = "SQLITE_IOERR_SHMLOCK",
                [(SQLitePCL.raw.SQLITE_IOERR | (21 << 8))] = "SQLITE_IOERR_SHMMAP",
                [(SQLitePCL.raw.SQLITE_IOERR | (22 << 8))] = "SQLITE_IOERR_SEEK",
                [(SQLitePCL.raw.SQLITE_IOERR | (23 << 8))] = "SQLITE_IOERR_DELETE_NOENT",
                [(SQLitePCL.raw.SQLITE_IOERR | (24 << 8))] = "SQLITE_IOERR_MMAP",
                [(SQLitePCL.raw.SQLITE_IOERR | (25 << 8))] = "SQLITE_IOERR_GETTEMPPATH",
                [(SQLitePCL.raw.SQLITE_IOERR | (26 << 8))] = "SQLITE_IOERR_CONVPATH",
                [(SQLitePCL.raw.SQLITE_IOERR | (27 << 8))] = "SQLITE_IOERR_VNODE",
                [(SQLitePCL.raw.SQLITE_IOERR | (28 << 8))] = "SQLITE_IOERR_AUTH",
                [(SQLitePCL.raw.SQLITE_IOERR | (29 << 8))] = "SQLITE_IOERR_BEGIN_ATOMIC",
                [(SQLitePCL.raw.SQLITE_IOERR | (30 << 8))] = "SQLITE_IOERR_COMMIT_ATOMIC",
                [(SQLitePCL.raw.SQLITE_IOERR | (31 << 8))] = "SQLITE_IOERR_ROLLBACK_ATOMIC",
                [(SQLitePCL.raw.SQLITE_IOERR | (32 << 8))] = "SQLITE_IOERR_DATA",
                [(SQLitePCL.raw.SQLITE_LOCKED | (1 << 8))] = "SQLITE_LOCKED_SHAREDCACHE",
                [(SQLitePCL.raw.SQLITE_LOCKED | (2 << 8))] = "SQLITE_LOCKED_VTAB",
                [(SQLitePCL.raw.SQLITE_BUSY | (1 << 8))] = "SQLITE_BUSY_RECOVERY",
                [(SQLitePCL.raw.SQLITE_BUSY | (2 << 8))] = "SQLITE_BUSY_SNAPSHOT",
                [(SQLitePCL.raw.SQLITE_BUSY | (3 << 8))] = "SQLITE_BUSY_TIMEOUT",
                [(SQLitePCL.raw.SQLITE_CANTOPEN | (1 << 8))] = "SQLITE_CANTOPEN_NOTEMPDIR",
                [(SQLitePCL.raw.SQLITE_CANTOPEN | (2 << 8))] = "SQLITE_CANTOPEN_ISDIR",
                [(SQLitePCL.raw.SQLITE_CANTOPEN | (3 << 8))] = "SQLITE_CANTOPEN_FULLPATH",
                [(SQLitePCL.raw.SQLITE_CANTOPEN | (4 << 8))] = "SQLITE_CANTOPEN_CONVPATH",
                [(SQLitePCL.raw.SQLITE_CANTOPEN | (5 << 8))] = "SQLITE_CANTOPEN_DIRTYWAL",
                [(SQLitePCL.raw.SQLITE_CANTOPEN | (6 << 8))] = "SQLITE_CANTOPEN_SYMLINK",
                [(SQLitePCL.raw.SQLITE_CORRUPT | (1 << 8))] = "SQLITE_CORRUPT_VTAB",
                [(SQLitePCL.raw.SQLITE_CORRUPT | (2 << 8))] = "SQLITE_CORRUPT_SEQUENCE",
                [(SQLitePCL.raw.SQLITE_CORRUPT | (3 << 8))] = "SQLITE_CORRUPT_INDEX",
                [(SQLitePCL.raw.SQLITE_READONLY | (1 << 8))] = "SQLITE_READONLY_RECOVERY",
                [(SQLitePCL.raw.SQLITE_READONLY | (2 << 8))] = "SQLITE_READONLY_CANTLOCK",
                [(SQLitePCL.raw.SQLITE_READONLY | (3 << 8))] = "SQLITE_READONLY_ROLLBACK",
                [(SQLitePCL.raw.SQLITE_READONLY | (4 << 8))] = "SQLITE_READONLY_DBMOVED",
                [(SQLitePCL.raw.SQLITE_READONLY | (5 << 8))] = "SQLITE_READONLY_CANTINIT",
                [(SQLitePCL.raw.SQLITE_READONLY | (6 << 8))] = "SQLITE_READONLY_DIRECTORY",
                [(SQLitePCL.raw.SQLITE_ABORT | (2 << 8))] = "SQLITE_ABORT_ROLLBACK",
                [(SQLitePCL.raw.SQLITE_CONSTRAINT | (1 << 8))] = "SQLITE_CONSTRAINT_CHECK",
                [(SQLitePCL.raw.SQLITE_CONSTRAINT | (2 << 8))] = "SQLITE_CONSTRAINT_COMMITHOOK",
                [(SQLitePCL.raw.SQLITE_CONSTRAINT | (3 << 8))] = "SQLITE_CONSTRAINT_FOREIGNKEY",
                [(SQLitePCL.raw.SQLITE_CONSTRAINT | (4 << 8))] = "SQLITE_CONSTRAINT_FUNCTION",
                [(SQLitePCL.raw.SQLITE_CONSTRAINT | (5 << 8))] = "SQLITE_CONSTRAINT_NOTNULL",
                [(SQLitePCL.raw.SQLITE_CONSTRAINT | (6 << 8))] = "SQLITE_CONSTRAINT_PRIMARYKEY",
                [(SQLitePCL.raw.SQLITE_CONSTRAINT | (7 << 8))] = "SQLITE_CONSTRAINT_TRIGGER",
                [(SQLitePCL.raw.SQLITE_CONSTRAINT | (8 << 8))] = "SQLITE_CONSTRAINT_UNIQUE",
                [(SQLitePCL.raw.SQLITE_CONSTRAINT | (9 << 8))] = "SQLITE_CONSTRAINT_VTAB",
                [(SQLitePCL.raw.SQLITE_CONSTRAINT | (10 << 8))] = "SQLITE_CONSTRAINT_ROWID",
                [(SQLitePCL.raw.SQLITE_CONSTRAINT | (11 << 8))] = "SQLITE_CONSTRAINT_PINNED",
                [(SQLitePCL.raw.SQLITE_NOTICE | (1 << 8))] = "SQLITE_NOTICE_RECOVER_WAL",
                [(SQLitePCL.raw.SQLITE_NOTICE | (2 << 8))] = "SQLITE_NOTICE_RECOVER_ROLLBACK",
                [(SQLitePCL.raw.SQLITE_WARNING | (1 << 8))] = "SQLITE_WARNING_AUTOINDEX",
                [(SQLitePCL.raw.SQLITE_AUTH | (1 << 8))] = "SQLITE_AUTH_USER",
                [(SQLitePCL.raw.SQLITE_OK | (1 << 8))] = "SQLITE_OK_LOAD_PERMANENTLY",
                [(SQLitePCL.raw.SQLITE_OK | (2 << 8))] = "SQLITE_OK_SYMLINK",
                // local to this api
                [short.MaxValue - 1] = "INVALID HANDLE",
                [short.MaxValue - 2] = "NULL COLUMN VALUE",
                [short.MaxValue - 3] = "INVALID COLUMN CAST",
                [short.MaxValue - 4] = "NON SQLITE EXCEPTION"

            };
        }

        /// <summary>InvalidHandle.</summary>
        public static readonly int InvalidHandle = short.MaxValue - 1;
        /// <summary>NullColumnValue.</summary>
        public static readonly int NullColumnValue = short.MaxValue - 2;
        /// <summary>InvalidColumnCast.</summary>
        public static readonly int InvalidColumnCast = short.MaxValue - 3;
        /// <summary>NonSqliteException.</summary>
        public static readonly int NonSqliteException = short.MaxValue - 4;

        /// <summary>Access the sqlite result code dictionary.</summary>
        public static ResultCodes Lookup => _instance.Value;

        /// <summary>Retrieve the name of <paramref name="resultCode"/>.</summary>
        public string this[int resultCode]
        {
            get
            {
                if (_lookup.TryGetValue(resultCode, out string? name))
                    return name;
                return $"{resultCode}";
            }
        }
    }

    /*
    /// <summary>Indicate either success or failure, and in the event of a failure, providing some idea of the cause of the failure.</summary>
    public enum PrimaryResult : int
    {
        /// <summary>The operation was successful and that there were no errors. Most other result codes indicate an error.</summary>
        SQLITE_OK = SQLitePCL.raw.SQLITE_OK,
        /// <summary>Generic error code that is used when no other more specific error code is available.</summary>
        SQLITE_ERROR = SQLitePCL.raw.SQLITE_ERROR,
        /// <summary>Indicates an internal malfunction. In a working version of SQLite, an application should never see this result code except from extensions or VFS's.</summary>
        SQLITE_INTERNAL = SQLitePCL.raw.SQLITE_INTERNAL,
        /// <summary>The requested access mode for a newly created database could not be provided</summary>
        SQLITE_PERM = SQLitePCL.raw.SQLITE_PERM,
        /// <summary>An operation was aborted prior to completion, usually be application request.</summary>
        SQLITE_ABORT = SQLitePCL.raw.SQLITE_ABORT,
        /// <summary>The database file could not be written (or in some cases read) because of concurrent activity by some other database connection, usually a database connection in a separate process.</summary>
        SQLITE_BUSY = SQLitePCL.raw.SQLITE_BUSY,
        /// <summary>A write operation could not continue because of a conflict within the same database connection or a conflict with a different database connection that uses a shared cache.</summary>
        SQLITE_LOCKED = SQLitePCL.raw.SQLITE_LOCKED,
        /// <summary>SQLite was unable to allocate all the memory it needed to complete the operation.</summary>
        SQLITE_NOMEM = SQLitePCL.raw.SQLITE_NOMEM,
        /// <summary>An attempt was made to alter some data for which the current database connection does not have write permission.</summary>
        SQLITE_READONLY = SQLitePCL.raw.SQLITE_READONLY,
        /// <summary>An operation was interrupted by the sqlite3_interrupt() interface.</summary>
        SQLITE_INTERRUPT = SQLitePCL.raw.SQLITE_INTERRUPT,
        /// <summary>The operation could not finish because the operating system reported an I/O error.</summary>
        SQLITE_IOERR = SQLitePCL.raw.SQLITE_IOERR,
        /// <summary>The database file has been corrupted.</summary>
        SQLITE_CORRUPT = SQLitePCL.raw.SQLITE_CORRUPT,
        /// <summary>VFS error.</summary>
        SQLITE_NOTFOUND = SQLitePCL.raw.SQLITE_NOTFOUND,
        /// <summary>
        /// A write could not complete because the disk is full.
        /// Note that this error can occur when trying to write information into the main database file, or it can also occur when writing into temporary disk files.
        /// Sometimes applications encounter this error even though there is an abundance of primary disk space
        /// because the error occurs when writing into temporary disk files on a system where temporary files are stored on a separate partition with much less space that the primary disk.
        /// </summary>
        SQLITE_FULL = SQLitePCL.raw.SQLITE_FULL,
        /// <summary>SQLite was unable to open a file. The file in question might be a primary database file or one of several temporary disk files.</summary>
        SQLITE_CANTOPEN = SQLitePCL.raw.SQLITE_CANTOPEN,
        /// <summary>A problem with the file locking protocol used by SQLite. The SQLITE_PROTOCOL error is currently only returned when using WAL mode and attempting to start a new transaction.</summary>
        SQLITE_PROTOCOL = SQLitePCL.raw.SQLITE_PROTOCOL,
        /// <summary>Currently not used.</summary>
        SQLITE_EMPTY = SQLitePCL.raw.SQLITE_EMPTY,
        /// <summary>The database schema has changed.</summary>
        SQLITE_SCHEMA = SQLitePCL.raw.SQLITE_SCHEMA,
        /// <summary>A string or BLOB was too large.</summary>
        SQLITE_TOOBIG = SQLitePCL.raw.SQLITE_TOOBIG,
        /// <summary>
        /// A SQL constraint violation occurred while trying to process an SQL statement.
        /// Additional information about the failed constraint can be found by consulting the accompanying error message (returned via sqlite3_errmsg() or sqlite3_errmsg16())
        /// or by looking at the extended error code.
        /// </summary>
        SQLITE_CONSTRAINT = SQLitePCL.raw.SQLITE_CONSTRAINT,
        /// <summary>
        /// A datatype mismatch returned in those few cases when the types do not match.
        /// SQLite is normally very forgiving about mismatches between the type of a value and the declared type of the container in which that value is to be stored.
        /// </summary>
        SQLITE_MISMATCH = SQLitePCL.raw.SQLITE_MISMATCH,
        /// <summary>Returned if the application uses any SQLite interface in a way that is undefined or unsupported.</summary>
        SQLITE_MISUSE = SQLitePCL.raw.SQLITE_MISUSE,
        /// <summary>Can be returned on systems that do not support large files when the database grows to be larger than what the filesystem can handle. "NOLFS" stands for "NO Large File Support".</summary>
        SQLITE_NOLFS = SQLitePCL.raw.SQLITE_NOLFS,
        /// <summary>The authorizer callback indicates that an SQL statement being prepared is not authorized.</summary>
        SQLITE_AUTH = SQLitePCL.raw.SQLITE_AUTH,
        /// <summary>Currently not used.</summary>
        SQLITE_FORMAT = SQLitePCL.raw.SQLITE_FORMAT,
        /// <summary>The parameter number argument to one of the sqlite3_bind routines or the column number in one of the sqlite3_column routines is out of range.</summary>
        SQLITE_RANGE = SQLitePCL.raw.SQLITE_RANGE,
        /// <summary>The file being opened does not appear to be an SQLite database file.</summary>
        SQLITE_NOTADB = SQLitePCL.raw.SQLITE_NOTADB,
        /// <summary>Sometimes used as the first argument in an sqlite3_log() callback to indicate that an unusual operation is taking place.</summary>
        SQLITE_NOTICE = SQLitePCL.raw.SQLITE_NOTICE,
        /// <summary>Sometimes used as the first argument in an sqlite3_log() callback to indicate that an unusual and possibly ill-advised operation is taking place.</summary>
        SQLITE_WARNING = SQLitePCL.raw.SQLITE_WARNING,
        /// <summary>Non-error returned by sqlite3_step() indicating that another row of output is available.</summary>
        SQLITE_ROW = SQLitePCL.raw.SQLITE_ROW,
        /// <summary>Non-error returned most commonly by sqlite3_step() indicating that the operation or sql statement has run to completion.</summary>
        SQLITE_DONE = SQLitePCL.raw.SQLITE_DONE
    }

    /// <summary>
    /// All extended result codes are also error codes.
    /// The primary result code is always a part of the extended result code.
    /// Given a full 32-bit extended result code, the application can always find the corresponding primary result code merely by extracting the least significant 8 bits of the extended result code.
    /// </summary>
    /// <example>
    /// <code>
    /// // IOERR_NOMEM (3082) is 12 shifted to the 8 significant bits and 10 (IOERR) in the 8 least significant bits
    /// IOErrorNoMem = (SqliteResult.IOError | (12 << 8))
    /// </code>
    /// </example>
    public enum ExtendedResult : int
    {
        SQLITE_ERROR_MISSING_COLLSEQ = (SQLitePCL.raw.SQLITE_ERROR | (1 << 8)),
        SQLITE_ERROR_RETRY = (SQLitePCL.raw.SQLITE_ERROR | (2 << 8)),
        SQLITE_ERROR_SNAPSHOT = (SQLitePCL.raw.SQLITE_ERROR | (3 << 8)),
        SQLITE_IOERR_READ = (SQLitePCL.raw.SQLITE_IOERR | (1 << 8)),
        SQLITE_IOERR_SHORT_READ = (SQLitePCL.raw.SQLITE_IOERR | (2 << 8)),
        SQLITE_IOERR_WRITE = (SQLitePCL.raw.SQLITE_IOERR | (3 << 8)),
        SQLITE_IOERR_FSYNC = (SQLitePCL.raw.SQLITE_IOERR | (4 << 8)),
        SQLITE_IOERR_DIR_FSYNC = (SQLitePCL.raw.SQLITE_IOERR | (5 << 8)),
        SQLITE_IOERR_TRUNCATE = (SQLitePCL.raw.SQLITE_IOERR | (6 << 8)),
        SQLITE_IOERR_FSTAT = (SQLitePCL.raw.SQLITE_IOERR | (7 << 8)),
        SQLITE_IOERR_UNLOCK = (SQLitePCL.raw.SQLITE_IOERR | (8 << 8)),
        SQLITE_IOERR_RDLOCK = (SQLitePCL.raw.SQLITE_IOERR | (9 << 8)),
        SQLITE_IOERR_DELETE = (SQLitePCL.raw.SQLITE_IOERR | (10 << 8)),
        SQLITE_IOERR_BLOCKED = (SQLitePCL.raw.SQLITE_IOERR | (11 << 8)),
        SQLITE_IOERR_NOMEM = (SQLitePCL.raw.SQLITE_IOERR | (12 << 8)),
        SQLITE_IOERR_ACCESS = (SQLitePCL.raw.SQLITE_IOERR | (13 << 8)),
        SQLITE_IOERR_CHECKRESERVEDLOCK = (SQLitePCL.raw.SQLITE_IOERR | (14 << 8)),
        SQLITE_IOERR_LOCK = (SQLitePCL.raw.SQLITE_IOERR | (15 << 8)),
        SQLITE_IOERR_CLOSE = (SQLitePCL.raw.SQLITE_IOERR | (16 << 8)),
        SQLITE_IOERR_DIR_CLOSE = (SQLitePCL.raw.SQLITE_IOERR | (17 << 8)),
        SQLITE_IOERR_SHMOPEN = (SQLitePCL.raw.SQLITE_IOERR | (18 << 8)),
        SQLITE_IOERR_SHMSIZE = (SQLitePCL.raw.SQLITE_IOERR | (19 << 8)),
        SQLITE_IOERR_SHMLOCK = (SQLitePCL.raw.SQLITE_IOERR | (20 << 8)),
        SQLITE_IOERR_SHMMAP = (SQLitePCL.raw.SQLITE_IOERR | (21 << 8)),
        SQLITE_IOERR_SEEK = (SQLitePCL.raw.SQLITE_IOERR | (22 << 8)),
        SQLITE_IOERR_DELETE_NOENT = (SQLitePCL.raw.SQLITE_IOERR | (23 << 8)),
        SQLITE_IOERR_MMAP = (SQLitePCL.raw.SQLITE_IOERR | (24 << 8)),
        SQLITE_IOERR_GETTEMPPATH = (SQLitePCL.raw.SQLITE_IOERR | (25 << 8)),
        SQLITE_IOERR_CONVPATH = (SQLitePCL.raw.SQLITE_IOERR | (26 << 8)),
        SQLITE_IOERR_VNODE = (SQLitePCL.raw.SQLITE_IOERR | (27 << 8)),
        SQLITE_IOERR_AUTH = (SQLitePCL.raw.SQLITE_IOERR | (28 << 8)),
        SQLITE_IOERR_BEGIN_ATOMIC = (SQLitePCL.raw.SQLITE_IOERR | (29 << 8)),
        SQLITE_IOERR_COMMIT_ATOMIC = (SQLitePCL.raw.SQLITE_IOERR | (30 << 8)),
        SQLITE_IOERR_ROLLBACK_ATOMIC = (SQLitePCL.raw.SQLITE_IOERR | (31 << 8)),
        SQLITE_IOERR_DATA = (SQLitePCL.raw.SQLITE_IOERR | (32 << 8)),
        SQLITE_LOCKED_SHAREDCACHE = (SQLitePCL.raw.SQLITE_LOCKED | (1 << 8)),
        SQLITE_LOCKED_VTAB = (SQLitePCL.raw.SQLITE_LOCKED | (2 << 8)),
        SQLITE_BUSY_RECOVERY = (SQLitePCL.raw.SQLITE_BUSY | (1 << 8)),
        SQLITE_BUSY_SNAPSHOT = (SQLitePCL.raw.SQLITE_BUSY | (2 << 8)),
        SQLITE_BUSY_TIMEOUT = (SQLitePCL.raw.SQLITE_BUSY | (3 << 8)),
        SQLITE_CANTOPEN_NOTEMPDIR = (SQLitePCL.raw.SQLITE_CANTOPEN | (1 << 8)),
        SQLITE_CANTOPEN_ISDIR = (SQLitePCL.raw.SQLITE_CANTOPEN | (2 << 8)),
        SQLITE_CANTOPEN_FULLPATH = (SQLitePCL.raw.SQLITE_CANTOPEN | (3 << 8)),
        SQLITE_CANTOPEN_CONVPATH = (SQLitePCL.raw.SQLITE_CANTOPEN | (4 << 8)),
        SQLITE_CANTOPEN_DIRTYWAL = (SQLitePCL.raw.SQLITE_CANTOPEN | (5 << 8)),
        SQLITE_CANTOPEN_SYMLINK = (SQLitePCL.raw.SQLITE_CANTOPEN | (6 << 8)),
        SQLITE_CORRUPT_VTAB = (SQLitePCL.raw.SQLITE_CORRUPT | (1 << 8)),
        SQLITE_CORRUPT_SEQUENCE = (SQLitePCL.raw.SQLITE_CORRUPT | (2 << 8)),
        SQLITE_CORRUPT_INDEX = (SQLitePCL.raw.SQLITE_CORRUPT | (3 << 8)),
        SQLITE_READONLY_RECOVERY = (SQLitePCL.raw.SQLITE_READONLY | (1 << 8)),
        SQLITE_READONLY_CANTLOCK = (SQLitePCL.raw.SQLITE_READONLY | (2 << 8)),
        SQLITE_READONLY_ROLLBACK = (SQLitePCL.raw.SQLITE_READONLY | (3 << 8)),
        SQLITE_READONLY_DBMOVED = (SQLitePCL.raw.SQLITE_READONLY | (4 << 8)),
        SQLITE_READONLY_CANTINIT = (SQLitePCL.raw.SQLITE_READONLY | (5 << 8)),
        SQLITE_READONLY_DIRECTORY = (SQLitePCL.raw.SQLITE_READONLY | (6 << 8)),
        SQLITE_ABORT_ROLLBACK = (SQLitePCL.raw.SQLITE_ABORT | (2 << 8)),
        SQLITE_CONSTRAINT_CHECK = (SQLitePCL.raw.SQLITE_CONSTRAINT | (1 << 8)),
        SQLITE_CONSTRAINT_COMMITHOOK = (SQLitePCL.raw.SQLITE_CONSTRAINT | (2 << 8)),
        SQLITE_CONSTRAINT_FOREIGNKEY = (SQLitePCL.raw.SQLITE_CONSTRAINT | (3 << 8)),
        SQLITE_CONSTRAINT_FUNCTION = (SQLitePCL.raw.SQLITE_CONSTRAINT | (4 << 8)),
        SQLITE_CONSTRAINT_NOTNULL = (SQLitePCL.raw.SQLITE_CONSTRAINT | (5 << 8)),
        SQLITE_CONSTRAINT_PRIMARYKEY = (SQLitePCL.raw.SQLITE_CONSTRAINT | (6 << 8)),
        SQLITE_CONSTRAINT_TRIGGER = (SQLitePCL.raw.SQLITE_CONSTRAINT | (7 << 8)),
        SQLITE_CONSTRAINT_UNIQUE = (SQLitePCL.raw.SQLITE_CONSTRAINT | (8 << 8)),
        SQLITE_CONSTRAINT_VTAB = (SQLitePCL.raw.SQLITE_CONSTRAINT | (9 << 8)),
        SQLITE_CONSTRAINT_ROWID = (SQLitePCL.raw.SQLITE_CONSTRAINT | (10 << 8)),
        SQLITE_CONSTRAINT_PINNED = (SQLitePCL.raw.SQLITE_CONSTRAINT | (11 << 8)),
        SQLITE_NOTICE_RECOVER_WAL = (SQLitePCL.raw.SQLITE_NOTICE | (1 << 8)),
        SQLITE_NOTICE_RECOVER_ROLLBACK = (SQLitePCL.raw.SQLITE_NOTICE | (2 << 8)),
        SQLITE_WARNING_AUTOINDEX = (SQLitePCL.raw.SQLITE_WARNING | (1 << 8)),
        SQLITE_AUTH_USER = (SQLitePCL.raw.SQLITE_AUTH | (1 << 8)),
        SQLITE_OK_LOAD_PERMANENTLY = (SQLitePCL.raw.SQLITE_OK | (1 << 8)),
        SQLITE_OK_SYMLINK = (SQLitePCL.raw.SQLITE_OK | (2 << 8))
    }
    
    */

}
