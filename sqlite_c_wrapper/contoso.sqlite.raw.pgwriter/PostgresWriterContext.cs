using System;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace contoso.sqlite.raw.pgwriter
{
    /// <summary>Wraps the Postgres binary copy operation such that callers do not need a direct dependency on the Npgsql library.</summary>
    public class PostgresWriterContext : IDisposable, IAsyncDisposable
    {
        readonly Func<ReadContext, PostgresBinaryWriter, Task> _rowWriter;
        readonly NpgsqlConnection _pgConnection;
        readonly NpgsqlBinaryImporter _pgWriter;
        readonly PostgresBinaryWriter _extendedWriter;
        readonly CancellationToken _cancelToken;
        bool _isAlreadyDisposed;

        internal PostgresWriterContext(NpgsqlConnection pgConnection, NpgsqlBinaryImporter pgWriter, Func<ReadContext, PostgresBinaryWriter, Task> rowWriter, CancellationToken cancelToken)
        {
            _rowWriter = rowWriter;
            _pgWriter = pgWriter;
            _pgConnection = pgConnection;
            _cancelToken = cancelToken;
            _extendedWriter = new PostgresBinaryWriter(_pgWriter);
        }

        /// <summary>
        /// Signature of the delegate required by the statement execution and row processing <see cref="TaskContext"/>.
        /// Caller accesses the binary writer supplied the by the Postgres api and writes out a row from a Sqlite reader
        /// to STDIN for the COPY command to consume and write to Postgres.
        /// </summary>
        public async Task CopyRow(ReadContext sqliteReader)
        {
            _cancelToken.ThrowIfCancellationRequested();
            await _rowWriter(sqliteReader, _extendedWriter);
        }

        /// <summary>
        /// Invokes 'Complete()' on the binary writer, which completes the bulk copy. The writer is unusable afterwards.
        /// Caller must call CommiteRows() (or <see cref="CommitRowsAsync"/> to save the data; 
        /// not doing so will cause the COPY operation to be rolled back when the writer is disposed 
        /// (which is also the expected default behavior when an exception is thrown).
        /// </summary>
        public ulong CommitRows()
        {
            if (_isAlreadyDisposed || _pgWriter == null)
                throw new ObjectDisposedException(this.GetType().Name);

            try
            {
                _cancelToken.ThrowIfCancellationRequested();
                return _pgWriter.Complete();
            }
            finally
            {
                try { _pgConnection?.Close(); }
                catch { }
            }
        }

        /// <summary>
        /// Invokes 'CompleteAsync()' on the binary writer, which completes the bulk copy and commits the rows sent to the database. The writer is unusable afterwards.
        /// Caller must call CommiteRowsAsync() (or <see cref="CommitRows"/> to save the data; 
        /// not doing so will cause the COPY operation to be rolled back when the writer is disposed 
        /// (which is also the expected default behavior when an exception is thrown).
        /// </summary>
        /// <remarks>
        /// Note that per Postgres COPY documentation, you might wish to invoke VACUUM after a large COPY operation that is rolled back, to recover wasted space.
        /// </remarks>
        public async Task<ulong> CommitRowsAsync()
        {
            if (_isAlreadyDisposed || _pgWriter == null)
                throw new ObjectDisposedException(this.GetType().Name);
            try
            {
                _cancelToken.ThrowIfCancellationRequested();
                return await _pgWriter.CompleteAsync(_cancelToken);
            }
            finally
            {
                try { await _pgConnection.CloseAsync().ConfigureAwait(false); }
                catch { }
            }
        }

        #region IDisposable 

        /// <summary>Closes the binary writer and returns the Postgres connection to the connection pool.</summary>
        protected virtual void Dispose(bool isManagedCall)
        {
            if (!_isAlreadyDisposed)
            {
                _isAlreadyDisposed = true;

                if (isManagedCall)
                {
                    try { _extendedWriter?.Dispose(); }
                    catch { }
                    try { _pgConnection?.Close(); }
                    catch { }
                }
            }
        }

        /// <summary>Closes the binary writer and returns the Postgres connection to the connection pool.</summary>
        protected virtual async ValueTask DisposeAsync(bool isManagedCall)
        {
            if (!_isAlreadyDisposed)
            {
                _isAlreadyDisposed = true;

                if (isManagedCall)
                {
                    if (_extendedWriter != null)
                    {
                        try { await _extendedWriter.DisposeAsync().ConfigureAwait(false); }
                        catch { }
                    }
                    if (_pgConnection != null)
                    {
                        try { await _pgConnection.CloseAsync().ConfigureAwait(false); }
                        catch { }
                    }
                }
            }
        }

        /// <summary>Closes the binary writer and returns the Postgres connection to the connection pool.</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Closes the binary writer and returns the Postgres connection to the connection pool.</summary>
        public ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return DisposeAsync(true);
        }

        #endregion
    }
}
