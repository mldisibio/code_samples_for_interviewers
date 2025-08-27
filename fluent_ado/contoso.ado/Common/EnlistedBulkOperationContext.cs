using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Runtime;
using System.Threading.Tasks;
using System.Xml;
using contoso.ado.EventListeners;
using contoso.ado.Fluent;
using contoso.ado.Internals;

namespace contoso.ado.Common
{
    /// <summary>
    /// Default implementation for the context wrapping the execution of repeated <see cref="DbCommand"/>
    /// INSERT statements (or similar), enlisted in and managed by a single <see cref="DbTransaction"/>. 
    /// The transaction is created from an already existing connection.
    /// The connection and transaction are then assigned to each command executed within the same connection as a single transaction.
    /// </summary>
    public class EnlistedBulkOperationContext<T>
    {
        readonly static Action<DbCommand> _cmdNoOp = _ => { };
        List<Action<DbCommand>>? _preExecuteQueue;
        List<Action<DbCommand>>? _postExecuteQueue;
        Func<ParameterContext, T, DbParameter[]>? _setNextParameters;

        /// <summary>True if 'Dispose' has already been called on this instance.</summary>
        protected bool _IsAlreadyDisposed;

        /// <summary>The <see cref="DbCommand"/> to be executed by this instance.</summary>
        readonly protected DbCommand _Command;

        /// <summary>The <see cref="Database"/> context for this instance.</summary>
        readonly protected Database _DbContext;

        /// <summary>The <see cref="TransactionContext"/> supporting <see cref="DbTransaction"/> semantics.</summary>
        readonly protected TransactionContext _TransactionContext;

        /// <summary>Instance of the factory class facilitating creation of an unconfigured <see cref="DbParameter"/>.</summary>
        readonly protected ParameterContext _CmdParameterFactory;

        /// <summary>Initialize with the given <see cref="Database"/> and <see cref="DbCommand"/>.</summary>
        protected internal EnlistedBulkOperationContext(DbCommand cmd, Database db, TransactionContext transactionCtx)
        {
            _DbContext = db.ThrowIfNull();
            _Command = cmd.ThrowIfNull();
            ParamCheck.Assert.IsNotNull(transactionCtx, "AdoTransactionContext");
            _TransactionContext = transactionCtx;
            _CmdParameterFactory = new ParameterContext(_DbContext);
        }

        /// <summary>Provides the underlying <see cref="IDataModuleObserver"/> instance to derived classes.</summary>
        protected ContextLog DataModuleObserver { get { return ContextLog.Q; } }

        #region Command Extensibility

        /// <summary>Returns a non null <see cref="PreExecute"/> delegate.</summary>
        protected Action<DbCommand> SafePreExecute => _preExecuteQueue == null ? _cmdNoOp : (cmd => _preExecuteQueue.ForEach(op => op(cmd)));

        /// <summary>Returns a non null <see cref="PostExecute"/> delegate.</summary>
        protected Action<DbCommand> SafePostExecute => _postExecuteQueue == null ? _cmdNoOp : (cmd => _postExecuteQueue.ForEach(op => op(cmd)));

        /// <summary>Add an extensibility delegate to be invoked after an opened connection is assigned to the command, but before the command is executed.</summary>
        protected void PreExecute(Action<DbCommand> cmdDelegate)
        {
            if (cmdDelegate != null)
            {
                _preExecuteQueue ??= new List<Action<DbCommand>>();
                _preExecuteQueue.Add(cmdDelegate);
            }
        }

        /// <summary>Add an xtensibility delegate to be invoke after the command is executed, but before its connection is disposed.</summary>
        protected void PostExecute(Action<DbCommand> cmdDelegate)
        {
            if (cmdDelegate != null)
            {
                _postExecuteQueue ??= new List<Action<DbCommand>>();
                _postExecuteQueue.Add(cmdDelegate);
            }
        }

        #endregion

        #region Parameter Configuration

        /// <summary>
        /// Applies the given delegate to create a collection of <see cref="DbParameter"/> using a <see cref="ParameterContext"/>
        /// and adds the parameters to the <see cref="DbCommand"/> in context.
        /// The delegate accepts a parameter source, which might be a data request object requiring interpretation or processing
        /// for conversion to a set of parameters.
        /// </summary>
        public EnlistedBulkOperationContext<T> WithParameters(Func<ParameterContext, T, DbParameter[]> setParameters)
        {
            ParamCheck.Assert.IsNotNull(setParameters, "setParameters delegate");
            _setNextParameters = setParameters;
            return this;
        }

        /// <summary>Add one or more <see cref="DbParameter"/>s to the <see cref="DbCommand"/> in context, or replace any pre-existing ones with the same name.</summary>
        protected void AddOrReplaceParameters(T nextIteration)
        {
            if (_setNextParameters != null)
            {
                DbParameter[] dbParameters = _setNextParameters(_CmdParameterFactory, nextIteration);
                if (dbParameters.IsNotNullOrEmpty())
                {
                    foreach (DbParameter dbParameter in dbParameters)
                    {
                        if (_Command.Parameters.Contains(dbParameter.ParameterName))
                            _Command.Parameters.RemoveAt(dbParameter.ParameterName);
                        _Command.Parameters.Add(dbParameter);
                    }
                }
            }
        }

        #endregion

        #region Mapping the Data Reader to DTO

        /// <summary>
        /// Uses the delegate <paramref name="mapRow"/> for mapping each <see cref="DbDataReader"/> record returned by the call to <see cref="M:System.Data.Common.DbCommand.ExecuteReader"/>
        /// to an instance of <typeparamref name="TOut"/>.
        /// </summary>
        public BulkReaderMapContext<T, TOut> WithMap<TOut>(Func<DbDataReader, TOut> mapRow)
        {
            ParamCheck.Assert.IsNotNull(mapRow, "mapRow");
            return new BulkReaderMapContext<T,TOut>(mapRow, this);
        }

        /// <summary>
        /// Uses the delegate <paramref name="mapRowAsync"/> for mapping each <see cref="DbDataReader"/> record returned by the call to <see cref="M:System.Data.Common.DbCommand.ExecuteReader"/>
        /// to an instance of <typeparamref name="TOut"/>.
        /// </summary>
        public BulkReaderMapTaskContext<T, TOut> WithMapTask<TOut>(Func<DbDataReader, Task<TOut>> mapRowAsync)
        {
            ParamCheck.Assert.IsNotNull(mapRowAsync, "mapRowAsync");
            return new BulkReaderMapTaskContext<T, TOut>(mapRowAsync, this);
        }

        #endregion

        #region Non-Reader Execution

        /// <summary>
        /// Invokes <see cref="M:System.Data.Common.DbCommand.ExecuteNonQuery"/> on the <see cref="DbCommand"/> 
        /// within one transaction for all items in <paramref name="items"/> and returns the number of rows affected.
        /// </summary>
        /// <returns>The number of individual executions with the bulk operation.</returns>
        public virtual int ExecuteNonQuery(IEnumerable<T> items)
        {
            // there is no bulk operation without a list of inputs
            if (items.IsNullOrEmpty())
                return 0;
            int counter = 0;
            try
            {
                _Command.Connection = _TransactionContext.Connection;
                _Command.Transaction = _TransactionContext.Transaction;
                _Command.Connection.Disposed += (s, e) => { try { _Command?.Dispose(); } catch { } };

                // setup first iteration
                T firstItem = items.First();
                AddOrReplaceParameters(firstItem);
                SafePreExecute(_Command);
                executePerItem(firstItem);
                ContextLog.Q.CommandExecuted(_Command);

                var remainingItems = items.Skip(1);
                foreach (T nextItem in remainingItems)
                {
                    executePerItem(nextItem);
                }

                ContextLog.Q.TraceDebug("{0} total bulk operations", counter);
                ContextLog.Q.CommandExecuted(_Command);
                SafePostExecute(_Command);
                return counter;
            }
            catch (Exception cmdEx)
            {
                _TransactionContext.RequestRollback();
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }

            void executePerItem(T nextItem)
            {
                AddOrReplaceParameters(nextItem);
                _Command.ExecuteNonQuery();
                counter++;
            }
        }

        /// <summary>
        /// Invokes <see cref="M:System.Data.Common.DbCommand.ExecuteNonQueryAsync"/> on the <see cref="DbCommand"/> 
        /// within one transaction for all items in <paramref name="items"/> and returns the number of rows affected.
        /// </summary>
        /// <returns>The number of individual executions with the bulk operation.</returns>
        public virtual async Task<int> ExecuteNonQueryAsync(IEnumerable<T> items)
        {
            // there is no bulk operation without a list of inputs
            if (items.IsNullOrEmpty())
                return 0;
            int counter = 0;
            try
            {
                _Command.Connection = _TransactionContext.Connection;
                _Command.Transaction = _TransactionContext.Transaction;
                _Command.Connection.Disposed += (s, e) => { try { _Command?.Dispose(); } catch { } };
                // setup first iteration
                T firstItem = items.First();
                AddOrReplaceParameters(firstItem);
                SafePreExecute(_Command);
                await executePerItem(firstItem).ConfigureAwait(false);
                ContextLog.Q.CommandExecuted(_Command);

                var remainingItems = items.Skip(1);
                foreach (T nextItem in remainingItems)
                {
                    await executePerItem(nextItem).ConfigureAwait(false);
                }

                ContextLog.Q.TraceDebug("{0} total bulk operations", counter);
                ContextLog.Q.CommandExecuted(_Command);
                SafePostExecute(_Command);
                return counter;
            }
            catch (Exception cmdEx)
            {
                _TransactionContext.RequestRollback();
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }

            async Task executePerItem(T nextItem)
            {
                AddOrReplaceParameters(nextItem);
                await _Command.ExecuteNonQueryAsync().ConfigureAwait(false);
                counter++;
            }
        }

        /// <summary>
        /// Invokes <see cref="M:System.Data.Common.DbCommand.ExecuteScalar"/> on the <see cref="DbCommand"/>
        /// within one transaction for all items in <paramref name="items"/> and returns an aggregate of the scalar results returned from each iteration of the bulk operation.
        /// Extra columns or rows are ignored.
        /// </summary>
        /// <returns>A collection of the scalar result returned from each iteration of the bulk operation.</returns>
        public virtual List<TOut?> ExecuteScalar<TOut>(IEnumerable<T> items, int preAllocation = 1024)
        {
            // there is no bulk operation without a list of inputs
            if (items.IsNullOrEmpty())
                return new List<TOut?>(0);

            int counter = 0;
            var results = new List<TOut?>(preAllocation);

            try
            {
                _Command.Connection = _TransactionContext.Connection;
                _Command.Transaction = _TransactionContext.Transaction;
                _Command.Connection.Disposed += (s, e) => { try { _Command?.Dispose(); } catch { } };
                // setup first iteration
                T firstItem = items.First();
                AddOrReplaceParameters(firstItem);
                SafePreExecute(_Command);
                results.Add(executePerItem(firstItem));
                ContextLog.Q.CommandExecuted(_Command);

                var remainingItems = items.Skip(1);
                foreach (T nextItem in remainingItems)
                {
                    results.Add(executePerItem(nextItem));
                }

                ContextLog.Q.TraceDebug("{0} total bulk operations", counter);
                ContextLog.Q.CommandExecuted(_Command);
                SafePostExecute(_Command);
                return results;
            }
            catch (Exception cmdEx)
            {
                _TransactionContext.RequestRollback();
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }

            TOut? executePerItem(T nextItem)
            {
                object? scalarResult = null;
                AddOrReplaceParameters(nextItem);
                // execute the command using ExecuteReader instead of ExecuteScalar
                // so that the correct results will be read for stored procedures that return intermediate select results
                // before the final scalar select is executed
                using (var reader = _Command.ExecuteReader())
                {
                    do
                    {
                        while (reader.Read())
                        {
                            scalarResult = reader.IsDBNull(0) ? null : reader.GetValue(0);
                        }
                    }
                    while (reader.NextResult());
                }
                counter++;
                return scalarResult == null ? default : (TOut?)scalarResult;
            }
        }

        /// <summary>
        /// Invokes <see cref="M:System.Data.Common.DbCommand.ExecuteScalarAsync"/> on the <see cref="DbCommand"/>
        /// within one transaction for all items in <paramref name="items"/> and returns an aggregate of the scalar results returned from each iteration of the bulk operation.
        /// Extra columns or rows are ignored.
        /// </summary>
        /// <returns>A collection of the scalar results returned from each iteration of the bulk operation.</returns>
        public virtual async Task<List<TOut?>> ExecuteScalarAsync<TOut>(IEnumerable<T> items, int preAllocation = 1024)
        {
            // there is no bulk operation without a list of inputs
            if (items.IsNullOrEmpty())
                return new List<TOut?>(0);

            int counter = 0;
            var results = new List<TOut?>(preAllocation);

            try
            {
                _Command.Connection = _TransactionContext.Connection;
                _Command.Transaction = _TransactionContext.Transaction;
                _Command.Connection.Disposed += (s, e) => { try { _Command?.Dispose(); } catch { } };
                // setup first iteration
                T firstItem = items.First();
                AddOrReplaceParameters(firstItem);
                SafePreExecute(_Command);
                results.Add(await executePerItem(firstItem).ConfigureAwait(false));
                ContextLog.Q.CommandExecuted(_Command);

                var remainingItems = items.Skip(1);
                foreach (T nextItem in remainingItems)
                {
                    results.Add(await executePerItem(nextItem).ConfigureAwait(false));
                }

                ContextLog.Q.TraceDebug("{0} total bulk operations", counter);
                ContextLog.Q.CommandExecuted(_Command);
                SafePostExecute(_Command);
                return results;
            }
            catch (Exception cmdEx)
            {
                _TransactionContext.RequestRollback();
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }

            async Task<TOut?> executePerItem(T nextItem)
            {
                object? scalarResult = null;
                AddOrReplaceParameters(nextItem);
                // execute the command using ExecuteReader instead of ExecuteScalar
                // so that the correct results will be read for stored procedures that return intermediate select results
                // before the final scalar select is executed
                DbDataReader reader;
                await using ((reader = await _Command.ExecuteReaderAsync()).ConfigureAwait(false))
                {
                    do
                    {
                        while (reader.Read())
                        {
                            scalarResult = reader.IsDBNull(0) ? null : reader.GetValue(0);
                        }
                    }
                    while (reader.NextResult());
                }
                counter++;
                return scalarResult == null ? default : (TOut?)scalarResult;
            }
        }

        #endregion

        #region Execute a Reader and Return First Row

        /// <summary>
        /// Invokes <see cref="M:System.Data.Common.DbCommand.ExecuteReader"/> on the <see cref="DbCommand"/> within one transaction
        /// for all items in <paramref name="items"/>, invokes <paramref name="mapRow"/> to map and return the first record of the <see cref="DbDataReader"/>
        /// as an instance of <typeparamref name="TOut"/>, and returns the aggregate collection of results per execution
        /// </summary>
        protected internal virtual List<TOut?> ExecuteScalarReader<TOut>(IEnumerable<T> items, Func<DbDataReader, TOut> mapRow, int preAllocation = 1024)
        {
            ParamCheck.Assert.IsNotNull(mapRow, "mapRow");
            // there is no bulk operation without a list of inputs
            if (items.IsNullOrEmpty())
                return new List<TOut?>(0);

            int counter = 0;
            var results = new List<TOut?>(preAllocation);

            try
            {
                _Command.Connection = _TransactionContext.Connection;
                _Command.Transaction = _TransactionContext.Transaction;
                _Command.Connection.Disposed += (s, e) => { try { _Command?.Dispose(); } catch { } };
                // setup first iteration
                T firstItem = items.First();
                AddOrReplaceParameters(firstItem);
                SafePreExecute(_Command);
                results.Add(executePerItem(firstItem));
                ContextLog.Q.CommandExecuted(_Command);

                var remainingItems = items.Skip(1);
                foreach (T nextItem in remainingItems)
                {
                    results.Add(executePerItem(nextItem));
                }

                ContextLog.Q.TraceDebug("{0} total bulk operations", counter);
                ContextLog.Q.CommandExecuted(_Command);
                SafePostExecute(_Command);
                return results;
            }
            catch (Exception cmdEx)
            {
                _TransactionContext.RequestRollback();
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }

            TOut? executePerItem(T nextItem)
            {
                TOut? result = default;
                AddOrReplaceParameters(nextItem);
                // execute and read first record, map the row and return the result;
                using (var reader = _Command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result = mapRow(reader);
                    }
                }
                counter++;
                return result;
            }
        }

        /// <summary>
        /// Invokes <see cref="M:System.Data.Common.DbCommand.ExecuteReaderAsync"/> on the <see cref="DbCommand"/> within one transaction
        /// for all items in <paramref name="items"/>, invokes <paramref name="mapRow"/> to map and return the first record of the <see cref="DbDataReader"/>
        /// as an instance of <typeparamref name="TOut"/>, and returns the aggregate collection of results per execution
        /// </summary>
        protected internal virtual async Task<List<TOut?>> ExecuteScalarReaderAsync<TOut>(IEnumerable<T> items, Func<DbDataReader, TOut> mapRow, int preAllocation = 1024)
        {
            ParamCheck.Assert.IsNotNull(mapRow, "mapRow");
            // there is no bulk operation without a list of inputs
            if (items.IsNullOrEmpty())
                return new List<TOut?>(0);

            int counter = 0;
            var results = new List<TOut?>(preAllocation);

            try
            {
                _Command.Connection = _TransactionContext.Connection;
                _Command.Transaction = _TransactionContext.Transaction;
                _Command.Connection.Disposed += (s, e) => { try { _Command?.Dispose(); } catch { } };
                // setup first iteration
                T firstItem = items.First();
                AddOrReplaceParameters(firstItem);
                SafePreExecute(_Command);
                results.Add(await executePerItem(firstItem).ConfigureAwait(false));
                ContextLog.Q.CommandExecuted(_Command);

                var remainingItems = items.Skip(1);
                foreach (T nextItem in remainingItems)
                {
                    results.Add(await executePerItem(nextItem).ConfigureAwait(false));
                }

                ContextLog.Q.TraceDebug("{0} total bulk operations", counter);
                ContextLog.Q.CommandExecuted(_Command);
                SafePostExecute(_Command);
                return results;
            }
            catch (Exception cmdEx)
            {
                _TransactionContext.RequestRollback();
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }

            async Task<TOut?> executePerItem(T nextItem)
            {
                TOut? result = default;
                AddOrReplaceParameters(nextItem);
                // execute and read first record, map the row and return the result;
                DbDataReader reader;
                await using ((reader = await _Command.ExecuteReaderAsync()).ConfigureAwait(false))
                {
                    if (reader.Read())
                    {
                        result = mapRow(reader);
                    }
                }
                counter++;
                return result;
            }
        }

        /// <summary>
        /// Invokes <see cref="M:System.Data.Common.DbCommand.ExecuteReaderAsync"/> on the <see cref="DbCommand"/> within one transaction
        /// for all items in <paramref name="items"/>, invokes <paramref name="mapRowAsync"/> to map and return the first record of the <see cref="DbDataReader"/>
        /// as an instance of <typeparamref name="TOut"/>, and returns the aggregate collection of results per execution
        /// </summary>
        protected internal virtual async Task<List<TOut?>> ExecuteScalarReaderAsync<TOut>(IEnumerable<T> items, Func<DbDataReader, Task<TOut>> mapRowAsync, int preAllocation = 1024)
        {
            ParamCheck.Assert.IsNotNull(mapRowAsync, "mapRowAsync");
            // there is no bulk operation without a list of inputs
            if (items.IsNullOrEmpty())
                return new List<TOut?>(0);

            int counter = 0;
            var results = new List<TOut?>(preAllocation);

            try
            {
                _Command.Connection = _TransactionContext.Connection;
                _Command.Transaction = _TransactionContext.Transaction;
                _Command.Connection.Disposed += (s, e) => { try { _Command?.Dispose(); } catch { } };
                // setup first iteration
                T firstItem = items.First();
                AddOrReplaceParameters(firstItem);
                SafePreExecute(_Command);
                results.Add(await executePerItem(firstItem).ConfigureAwait(false));
                ContextLog.Q.CommandExecuted(_Command);

                var remainingItems = items.Skip(1);
                foreach (T nextItem in remainingItems)
                {
                    results.Add(await executePerItem(nextItem).ConfigureAwait(false));
                }

                ContextLog.Q.TraceDebug("{0} total bulk operations", counter);
                ContextLog.Q.CommandExecuted(_Command);
                SafePostExecute(_Command);
                return results;
            }
            catch (Exception cmdEx)
            {
                _TransactionContext.RequestRollback();
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }

            async Task<TOut?> executePerItem(T nextItem)
            {
                TOut? result = default;
                AddOrReplaceParameters(nextItem);
                // execute and read first record, map the row and return the result;
                DbDataReader reader;
                await using ((reader = await _Command.ExecuteReaderAsync()).ConfigureAwait(false))
                {
                    if (reader.Read())
                    {
                        result = await mapRowAsync(reader).ConfigureAwait(false);
                    }
                }
                counter++;
                return result;
            }
        }

        #endregion


    }

    /// <summary>
    /// Simple fluent artifact to guide command execution path when a delegate for mapping an
    /// <see cref="DbDataReader"/> record is specified as part of the caller's command context.
    /// </summary>
    public sealed class BulkReaderMapContext<TIn, TOut>
    {
        internal readonly EnlistedBulkOperationContext<TIn> CommandContext;
        internal readonly Func<DbDataReader, TOut> MapRow;

        /// <summary>Initialize with the items needed to preserve the overall command context and support a fluent syntax.</summary>
        internal BulkReaderMapContext(Func<DbDataReader, TOut> mapRow, EnlistedBulkOperationContext<TIn> cmdContext)
        {
            CommandContext = cmdContext;
            MapRow = mapRow;
        }

        /// <summary>Executes the <see cref="DbCommand"/> in context for each item in <paramref name="items"/> within one transaction.</summary>
        /// <returns>A collection of each <typeparamref name="TOut"/> returned from each iteration of the bulk operation, as mapped by <see cref="MapRow"/>.</returns>
        public List<TOut?> ExecuteScalarReader(IEnumerable<TIn> items, int preAllocation = 1024) => CommandContext.ExecuteScalarReader(items, MapRow, preAllocation);

        /// <summary>Executes the <see cref="DbCommand"/> in context for each item in <paramref name="items"/> within one transaction.</summary>
        /// <returns>A collection of each <typeparamref name="TOut"/> returned from each iteration of the bulk operation, as mapped by <see cref="MapRow"/>.</returns>
        public Task<List<TOut?>> ExecuteScalarReaderAsync(IEnumerable<TIn> items, int preAllocation = 1024) => CommandContext.ExecuteScalarReaderAsync(items, MapRow, preAllocation);
    }

    /// <summary>
    /// Simple fluent artifact to guide command execution path when a task for mapping an
    /// <see cref="DbDataReader"/> record is specified as part of the caller's command context.
    /// </summary>
    public sealed class BulkReaderMapTaskContext<TIn, TOut>
    {
        internal readonly EnlistedBulkOperationContext<TIn> CommandContext;
        internal readonly Func<DbDataReader, Task<TOut>> MapRowTask;

        /// <summary>Initialize with the items needed to preserve the overall command context and support a fluent syntax.</summary>
        internal BulkReaderMapTaskContext(Func<DbDataReader, Task<TOut>> mapRowTask, EnlistedBulkOperationContext<TIn> cmdContext)
        {
            CommandContext = cmdContext;
            MapRowTask = mapRowTask;
        }

        /// <summary>Executes the <see cref="DbCommand"/> in context for each item in <paramref name="items"/> within one transaction.</summary>
        /// <returns>A collection of each <typeparamref name="TOut"/> returned from each iteration of the bulk operation, as mapped by <see cref="MapRowTask"/>.</returns>
        public Task<List<TOut?>> ExecuteScalarReaderAsync(IEnumerable<TIn> items, int preAllocation = 1024) => CommandContext.ExecuteScalarReaderAsync(items, MapRowTask, preAllocation);
    }
}
