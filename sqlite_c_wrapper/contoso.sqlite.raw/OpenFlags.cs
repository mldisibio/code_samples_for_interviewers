/*
using System;

namespace contoso.sqlite.raw
{
    /// <summary>
    /// Flags passed to the sqlite3_open_v2() method.
    /// </summary>
    [Flags]
    public enum SQLiteOpenFlags
    {
        /// <summary>The database is opened in read-only mode. If the database does not already exist, an error is returned.</summary>
        ReadOnly = 1,
        /// <summary>
        /// The database is opened for reading and writing if possible, or reading only if the file is write protected by the operating system. 
        /// In either case the database must already exist, otherwise an error is returned.
        /// </summary>
        ReadWrite = 2,
        /// <summary>The database is created if it does not already exist. Commonly combined with ReadWrite flag.</summary>
        Create = 4,
        /// <summary>The filename can be interpreted as a URI.</summary>
        Uri = 0x40,
        /// <summary>
        /// The database will be opened as an in-memory database.
        /// The database is named by the "filename" argument for the purposes of cache-sharing, if shared cache mode is enabled, but the "filename" is otherwise ignored.
        /// </summary>
        Memory = 0x80,
        /// <summary>
        /// The new database connection will use the "multi-thread" threading mode. 
        /// This means that separate threads are allowed to use SQLite at the same time, as long as each thread is using a different database connection.
        /// </summary>
        NoMutex = 0x8000,
        /// <summary>
        /// The new database connection will use the "serialized" threading mode. This means the multiple threads can safely attempt to use the same database connection at the same time. 
        /// (Mutexes will block any actual concurrency, but in this mode there is no harm in trying.)
        /// </summary>
        FullMutex = 0x10000,
        /// <summary>
        /// The database is opened shared cache enabled (disabled by default)
        /// Intended for use in embedded servers. If shared-cache mode is enabled and a thread or process establishes multiple connections to the same database, 
        /// the connections share a single data and schema cache.
        /// </summary>
        SharedCache = 0x20000,
        /// <summary>The database is opened shared cache disabled.</summary>
        PrivateCache = 0x40000
    }

    /// <summary>Defines access to the sqlite file.</summary>
    public enum OpenMode
    {
        /// <summary>The database is opened in read-only mode. If the database does not already exist, an error is returned.</summary>
        ReadOnly = 1,
        /// <summary>
        /// The database is opened for reading and writing if possible, or reading only if the file is write protected by the operating system. 
        /// In either case the database must already exist, otherwise an error is returned.
        /// </summary>
        ReadWrite = 2,
        /// <summary>The database is opened for reading and writing, and is created if it does not already exist.</summary>
        ReadWriteCreate = 6
    }

    /// <summary>Defines access to data and schema cache.</summary>
    public enum CacheMode
    {
        /// <summary>
        /// The database is opened shared cache enabled (disabled by default)
        /// Intended for use in embedded servers. If shared-cache mode is enabled and a thread or process establishes multiple connections to the same database, 
        /// the connections share a single data and schema cache.
        /// </summary>
        SharedCache = 0x20000,
        /// <summary>The database is opened shared cache disabled.</summary>
        PrivateCache = 0x40000
    }

    /// <summary>Defines thread safety assuming single-thread mode has not been applied.</summary>
    public enum ThreadMode
    {
        /// <summary>
        /// The new database connection will use the "multi-thread" threading mode. 
        /// This means that separate threads are allowed to use SQLite at the same time, as long as each thread is using a different database connection.
        /// </summary>
        NoMutex = 0x8000,
        /// <summary>
        /// The new database connection will use the "serialized" threading mode. This means the multiple threads can safely attempt to use the same database connection at the same time. 
        /// (Mutexes will block any actual concurrency, but in this mode there is no harm in trying.)
        /// </summary>
        FullMutex = 0x10000,
    }
}
*/
