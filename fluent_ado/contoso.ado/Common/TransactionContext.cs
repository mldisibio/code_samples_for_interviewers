using System;
using System.Data.Common;
using System.Threading.Tasks;
using contoso.ado.Internals;

namespace contoso.ado.Common
{
    /// <summary>Manages a single connection and local transaction for one or more commands that are executed within its scope.</summary>
    public sealed class TransactionContext : IDisposable, IAsyncDisposable
    {
        bool _rollbackRequested;
        bool _isAlreadyDisposed;

        TransactionContext(Database dbContext)
        {
            Connection = dbContext.CreateConnection();
        }

        /// <summary>The opened connection on which all related local transactions should run.</summary>
        public DbConnection Connection { get; }

        /// <summary>The <see cref="DbTransaction"/> for each <see cref="DbCommand"/> in scope of this instance.</summary>
        public DbTransaction? Transaction { get; private set; }

        /// <summary>Returns any exception that was encountered during Commit or Rollback.</summary>
        public Exception? Error { get; private set; }

        /// <summary>True if an exception was encountered during Commit or Rollback.</summary>
        public bool HasError { get { return Error != null; } }

        /// <summary>True if the transaction is null or disposed.</summary>
        internal bool ContextInvalid { get { return Transaction == null || _isAlreadyDisposed; } }

        /// <summary>True if a rollback is already pending for this transaction or the context is no longer valid.</summary>
        /// <remarks>Useful for opting out of execution of the next command if one or more previous commands withing the transaction has already failed.</remarks>
        public bool ContextAborted { get { return ContextInvalid || _rollbackRequested; } }

        /// <summary>Initiates the scope for an open connection and transaction. This constructor call should be wrapped in a <see langword="using"/> statement.</summary>
        internal static TransactionContext Create(Database dbContext)
        {
            var ctx = new TransactionContext(dbContext);
            ctx.Initialize();
            return ctx;
        }

        /// <summary>Initiates the scope for an open connection and transaction. This constructor call should be wrapped in a <see langword="using"/> statement.</summary>
        internal static async Task<TransactionContext> CreateAsync(Database dbContext)
        {
            var ctx = new TransactionContext(dbContext);
            await ctx.InitializeAsync().ConfigureAwait(false);
            return ctx;
        }

        /// <summary>The transaction will be rolled back when the context goes out of scope.</summary>
        public void RequestRollback()
        {
            if (_isAlreadyDisposed)
                throw new ObjectDisposedException("AdoTransactionContext");

            if (_rollbackRequested == false)
                _rollbackRequested = true;
        }

        void Initialize()
        {
            if (_isAlreadyDisposed)
                throw new ObjectDisposedException("AdoTransactionContext");

            try
            {
                Connection.Open();
                Transaction = Connection.BeginTransaction();
            }
            catch (Exception initEx)
            {
                try
                {
                    if (Transaction != null)
                        RollbackTransaction();
                    ContextLog.Q.ConnectionFailed("Failed to open an ADO.Net Transaction on the given connection.", initEx);
                }
                finally
                {
                    Connection.SafeClose();
                }
                throw;
            }
        }

        async Task InitializeAsync()
        {
            if (_isAlreadyDisposed)
                throw new ObjectDisposedException("AdoTransactionContext");

            try
            {
                await Connection.OpenAsync().ConfigureAwait(false);
                Transaction = Connection.BeginTransaction();
            }
            catch (Exception initEx)
            {
                try
                {
                    if (Transaction != null)
                        RollbackTransaction();
                    ContextLog.Q.ConnectionFailed("Failed to open an ADO.Net Transaction on the given connection.", initEx);
                }
                finally
                {
                    Connection.SafeClose();
                }
                throw;
            }
        }

        /// <summary>Commits the local transaction, or rolls it back if so requested, and closes the connection.</summary>
        public void Dispose()
        {
            if (!_isAlreadyDisposed)
            {
                try
                {
                    _isAlreadyDisposed = true;

                    if (_rollbackRequested)
                        RollbackTransaction();
                    else
                        CommitTransaction();
                }
                finally
                {
                    if (Transaction != null)
                    {
                        try { Transaction.Dispose(); }
                        catch { }
                    }
                    Connection.SafeClose();
                }
            }
        }

        /// <summary>Commits the local transaction, or rolls it back if so requested, and closes the connection.</summary>
        public async ValueTask DisposeAsync()
        {
            Dispose();
            await Task.CompletedTask;
        }

        void CommitTransaction()
        {
            if (Transaction != null)
            {
                try
                {
                    Transaction.Commit();
                    ContextLog.Q.TraceDebug("Committed all statements executed under the current ADO.Net Transaction scope.");
                }
                catch (Exception commitEx)
                {
                    Error = commitEx;
                    ContextLog.Q.TraceError(commitEx, "Failed to commit the current ADO.Net Transaction. Attempting to rollback...");
                    RollbackTransaction();
                }
            }
            else
                ContextLog.Q.TraceError(new ArgumentNullException(), "Failed to commit the current ADO.Net Transaction. The transaction instance is null.");

        }

        void RollbackTransaction()
        {
            if (Transaction != null)
            {
                try
                {
                    Transaction.Rollback();
                    ContextLog.Q.TraceInfo("Rollback for all local transactions executed under the current ADO.Net Transaction scope.");
                }
                catch (Exception rollbackEx)
                {
                    if (_rollbackRequested)
                    {
                        Error = rollbackEx;
                        // more explicit logging, since a Rollback was requested.
                        ContextLog.Q.TraceError(rollbackEx, "Failed to rollback the current ADO.Net Transaction.");
                    }
                    else
                        // less explicit logging, since this would be clean-up and could easily fail if there was nothing to rollback
                        ContextLog.Q.TraceError(null, "Failed to rollback the current ADO.Net Transaction: {0}", rollbackEx.Message);
                }
            }
            else
                ContextLog.Q.TraceError(new ArgumentNullException(), "Failed to rollback the current ADO.Net Transaction. The transaction instance is null.");

        }
    }
}
