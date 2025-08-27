/*
using System;

namespace contoso.sqlite.raw
{    
    /// <summary>
    /// Defines the threading mode passed into sqlite3_config(), used to make global configuration changes to SQLite. 
    /// (May only be invoked prior to library initialization using sqlite3_initialize() or after shutdown by sqlite3_shutdown())
    /// Threading modes can be selected at build, initialization, or on connection creation.
    /// However, SingleThread cannot be overridden once chosen at build, nor can a connection downgrade to SingleThread (but initialization can).
    /// </summary>
    public enum ThreadingMode : int
    {
        /// <summary>
        /// All mutexes are disabled and SQLite is unsafe to use in more than a single thread at once.
        /// Critical mutexing logic is omitted from the build. Only one thread can be used
        /// </summary>
        SingleThread = 1,
        /// <summary>
        /// Disables mutexing on database connection and prepared statement objects. The application is responsible for serializing access to database connections and prepared statements. 
        /// But other mutexes are enabled so that SQLite will be safe to use in a multi-threaded environment as long as no two threads attempt to use the same database connection (handle) at the same time.
        /// 
        /// For 3.3.1 (2006) A connection can only be passed to another thread when any outstanding statements are closed and finalized. 
        /// In practice this means that it is not possible to keep a prepared statement in memory for later executions.
        /// It seems that in 3.5.0 (2007) requirement to dispose prepared statements before sharing a connection is relaxed. 
        /// But prepared statements are still specific to a connection and if you have a connection 'pool' you cannot guarantee which connection you will be given.
        /// </summary>
        MultiThread = 2,
        /// <summary>
        /// Enables all mutexes including the recursive mutexes on database connection and prepared statement objects. 
        /// In this mode (default) the SQLite library will itself serialize access to database connections and prepared statements 
        /// so that the application is free to use the same database connection or the same prepared statement in different threads at the same time.
        /// Also called Full Mutex because mutexes will block any actual concurrency.
        /// </summary>
        Serialized = 3
    }
}
*/
