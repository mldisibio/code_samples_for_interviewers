using System;
using System.Diagnostics;

namespace contoso.sqlite.raw
{
    /// <summary>Accessibility to the open connection and file path for all operations.</summary>
    public abstract class SqliteContext : IDisposable
    {
        /// <summary>Track if Disposed was invoked.</summary>
        protected bool _isAlreadyDisposed;

        /// <summary>Initialize with an open sqlite connection and the identifier of the opened file.</summary>
        protected SqliteContext(SQLitePCL.sqlite3 dbHandle, string filePath)
        {
            DbHandle = dbHandle;
            FilePath = filePath;
        }

        /// <summary>Intialize as an existing <see cref="SqliteContext"/></summary>
        protected SqliteContext(SqliteContext ctx)
            : this(ctx.DbHandle, ctx.FilePath) { }

        /// <summary>The opened database handle managed by this context instance.</summary>
        internal SQLitePCL.sqlite3 DbHandle { get; }

        /// <summary>The path or identifier for the sqlite database in context.</summary>
        public string FilePath { get; }

        /// <summary>Throw <see cref="ObjectDisposedException"/> if already disposed.</summary>
        protected virtual void ThrowIfDisposed()
        {
            if(_isAlreadyDisposed)
                throw new ObjectDisposedException(new StackFrame(1).GetMethod()?.DeclaringType?.Name);
        }

        /// <summary>Clean up resources. Closes the sqlite3 handle.</summary>
        protected virtual void Dispose(bool isManagedCall)
        {
            if (!_isAlreadyDisposed)
            {
                _isAlreadyDisposed = true;
                // defer resource managment to child classes;
                // we don't want to release DbHandle except when Connection instances are disposed
            }
        }

        /// <summary>Clean up resources.</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
