using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Threading.Tasks;
using contoso.ado.Internals;

namespace contoso.ado.Common
{
    /// <summary>
    /// Default implementation for the context wrapping the execution of a <see cref="DbCommand"/>
    /// common to the Ado.Net framework, enlisted in and managed by a <see cref="DbTransaction"/>. 
    /// The transaction is created from an already existing connection.
    /// The connection and transaction are then assigned to this command and optionally to any other
    /// commands executed within the same connection as a single transaction.
    /// Other provider specific Command implementations can inherit from this implementation and need only
    /// override provider specific functionality.
    /// </summary>
    public class EnlistedCommandContext : CommandContext
    {
        /// <summary>The <see cref="TransactionContext"/> supporting <see cref="DbTransaction"/> semantics.</summary>
        readonly protected TransactionContext _TransactionContext;

        /// <summary>Initialize with the given <see cref="Database"/> and <see cref="DbCommand"/>.</summary>
        protected internal EnlistedCommandContext(DbCommand cmd, Database db, TransactionContext transactionCtx)
            : base(cmd, db)
        {
            ParamCheck.Assert.IsNotNull(transactionCtx, "AdoTransactionContext");
            _TransactionContext = transactionCtx;
        }

        #region Non-Reader Execution

        /// <summary>
        /// Invokes <see cref="M:System.Data.Common.DbCommand.ExecuteNonQuery"/> on the <see cref="DbCommand"/> 
        /// within a transaction and returns the number of rows affected.
        /// </summary>
        /// <returns>The number of rows affected.</returns>
        public override int ExecuteNonQuery()
        {
            try
            {
                _Command.Connection = _TransactionContext.Connection;
                _Command.Transaction = _TransactionContext.Transaction;
                SafePreExecute(_Command);
                int result = _Command.ExecuteNonQuery();
                SafePostExecute(_Command);
                ContextLog.Q.CommandExecuted(_Command);
                return result;
            }
            catch (Exception cmdEx)
            {
                _TransactionContext.RequestRollback();
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
        }

        /// <summary>
        /// Invokes <see cref="M:System.Data.Common.DbCommand.ExecuteNonQueryAsync"/> on the <see cref="DbCommand"/>
        /// within a transaction and returns the number of rows affected.
        /// </summary>
        /// <returns>The number of rows affected.</returns>
        public override async Task<int> ExecuteNonQueryAsync()
        {
            try
            {
                _Command.Connection = _TransactionContext.Connection;
                _Command.Transaction = _TransactionContext.Transaction;
                SafePreExecute(_Command);
                int result = await _Command.ExecuteNonQueryAsync().ConfigureAwait(false);
                SafePostExecute(_Command);
                ContextLog.Q.CommandExecuted(_Command);
                return result;
            }
            catch (Exception cmdEx)
            {
                _TransactionContext.RequestRollback();
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
        }

        /// <summary>
        /// Invokes <see cref="M:System.Data.Common.DbCommand.ExecuteScalar"/> on the <see cref="DbCommand"/>
        /// within a transaction and returns the first column of the first row in the last result set returned by the query.
        /// Extra columns or rows are ignored.
        /// </summary>
        /// <returns>The first column of the first row in the last result set, or a <see langword="null"/> reference.</returns>
        public override T? ExecuteScalar<T>() where T : default
        {
            object? scalarResult = null;
            try
            {
                _Command.Connection = _TransactionContext.Connection;
                _Command.Transaction = _TransactionContext.Transaction;
                SafePreExecute(_Command);

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

                SafePostExecute(_Command);
                ContextLog.Q.CommandExecuted(_Command);
                return scalarResult == null ? default : (T?)scalarResult;
            }
            catch (Exception cmdEx)
            {
                _TransactionContext.RequestRollback();
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
        }

        /// <summary>
        /// Invokes <see cref="M:System.Data.Common.DbCommand.ExecuteScalarAsync"/> on the <see cref="DbCommand"/>
        /// within a transaction and returns the first column of the first row in the last result set returned by the query.
        /// Extra columns or rows are ignored.
        /// </summary>
        /// <returns>The first column of the first row in the last result set, or a <see langword="null"/> reference.</returns>
        public override async Task<T?> ExecuteScalarAsync<T>() where T : default
        {
            object? scalarResult = null;
            try
            {
                _Command.Connection = _TransactionContext.Connection;
                _Command.Transaction = _TransactionContext.Transaction;
                SafePreExecute(_Command);

                // execute the command asynchronously using ExecuteReader instead of ExecuteScalar
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
                    while (await reader.NextResultAsync().ConfigureAwait(false));
                }

                SafePostExecute(_Command);
                ContextLog.Q.CommandExecuted(_Command);
                return default; // scalarResult == null ? default : (T)scalarResult;
            }
            catch (Exception cmdEx)
            {
                _TransactionContext.RequestRollback();
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
        }

        #endregion

        #region Execute a Reader and Process Results

        /// <summary>
        /// Invokes <see cref="M:System.Data.Common.DbCommand.ExecuteReader"/> on the <see cref="DbCommand"/> within a transaction,
        /// invokes <paramref name="mapRow"/> to map each record of the <see cref="DbDataReader"/> to an instance of <typeparamref name="TOut"/>,
        /// and then invokes the action <paramref name="processDataObject"/> on each mapped row.
        /// </summary>
        protected internal override void ExecuteReader<TOut>(Func<DbDataReader, TOut> mapRow, Action<TOut> processDataObject)
        {
            ParamCheck.Assert.IsNotNull(mapRow, "mapRow");
            ParamCheck.Assert.IsNotNull(processDataObject, "processDataObject");

            try
            {
                _Command.Connection = _TransactionContext.Connection;
                _Command.Transaction = _TransactionContext.Transaction;
                SafePreExecute(_Command);

                // execute and iterate the reader, map each row and process the results;
                // when done, the connection is closed
                using (DbDataReader reader = _Command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        TOut dto = mapRow(reader);
                        processDataObject(dto);
                    }
                }

                SafePostExecute(_Command);
                ContextLog.Q.CommandExecuted(_Command);
            }
            catch (Exception cmdEx)
            {
                _TransactionContext.RequestRollback();
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
        }

        /// <summary>
        /// Returns a task that invokes <see cref="M:System.Data.Common.DbCommand.ExecuteReaderAsync"/> on the <see cref="DbCommand"/> within a transaction,
        /// invokes <paramref name="mapRow"/> to map each record of the <see cref="DbDataReader"/> to an instance of <typeparamref name="TOut"/>,
        /// and then invokes the action <paramref name="processDataObject"/> on each mapped row.
        /// </summary>
        protected internal override async Task ExecuteReaderAsync<TOut>(Func<DbDataReader, TOut> mapRow, Action<TOut> processDataObject)
        {
            ParamCheck.Assert.IsNotNull(mapRow, "mapRow");
            ParamCheck.Assert.IsNotNull(processDataObject, "processDataObject");

            try
            {
                _Command.Connection = _TransactionContext.Connection;
                _Command.Transaction = _TransactionContext.Transaction;
                SafePreExecute(_Command);

                // execute and iterate the reader, map each row and process the results;
                // when done, the connection is closed
                DbDataReader reader;
                await using ((reader = await _Command.ExecuteReaderAsync()).ConfigureAwait(false))
                {
                    while (reader.Read())
                    {
                        TOut dto = mapRow(reader);
                        processDataObject(dto);
                    }
                }

                SafePostExecute(_Command);
                ContextLog.Q.CommandExecuted(_Command);
            }
            catch (Exception cmdEx)
            {
                _TransactionContext.RequestRollback();
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
        }

        /// <summary>
        /// Returns a task that invokes <see cref="M:System.Data.Common.DbCommand.ExecuteReaderAsync"/> on the <see cref="DbCommand"/> within a transaction,
        /// invokes <paramref name="mapRow"/> to map each record of the <see cref="DbDataReader"/> to an instance of <typeparamref name="TOut"/>,
        /// and awaits the <paramref name="processDataObjectAsync"/> task on each mapped row.
        /// </summary>
        protected internal override async Task ExecuteReaderAsync<TOut>(Func<DbDataReader, TOut> mapRow, Func<TOut, Task> processDataObjectAsync)
        {
            ParamCheck.Assert.IsNotNull(mapRow, "mapRow");
            ParamCheck.Assert.IsNotNull(processDataObjectAsync, "processDataObjectAsync");

            try
            {
                _Command.Connection = _TransactionContext.Connection;
                _Command.Transaction = _TransactionContext.Transaction;
                SafePreExecute(_Command);

                // execute and iterate the reader, map each row and process the results;
                // when done, the connection is closed
                DbDataReader reader;
                await using ((reader = await _Command.ExecuteReaderAsync()).ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        TOut dto = mapRow(reader);
                        await processDataObjectAsync(dto).ConfigureAwait(false);
                    }
                }

                SafePostExecute(_Command);
                ContextLog.Q.CommandExecuted(_Command);
            }
            catch (Exception cmdEx)
            {
                _TransactionContext.RequestRollback();
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
        }

        /// <summary>
        /// Returns a task that invokes <see cref="M:System.Data.Common.DbCommand.ExecuteReaderAsync"/> on the <see cref="DbCommand"/> within a transaction,
        /// awaits <paramref name="mapRowAsync"/> to map each record of the <see cref="DbDataReader"/> to an instance of <typeparamref name="TOut"/>,
        /// and invokes the action <paramref name="processDataObject"/> on each mapped row.
        /// </summary>
        protected internal override async Task ExecuteReaderAsync<TOut>(Func<DbDataReader, Task<TOut>> mapRowAsync, Action<TOut> processDataObject)
        {
            ParamCheck.Assert.IsNotNull(mapRowAsync, "mapRowAsync");
            ParamCheck.Assert.IsNotNull(processDataObject, "processDataObject");

            try
            {
                _Command.Connection = _TransactionContext.Connection;
                _Command.Transaction = _TransactionContext.Transaction;
                SafePreExecute(_Command);

                // execute and iterate the reader, map each row and process the results;
                // when done, the connection is closed
                DbDataReader reader;
                await using ((reader = await _Command.ExecuteReaderAsync()).ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        TOut dto = await mapRowAsync(reader).ConfigureAwait(false);
                        processDataObject(dto);
                    }
                }

                SafePostExecute(_Command);
                ContextLog.Q.CommandExecuted(_Command);
            }
            catch (Exception cmdEx)
            {
                _TransactionContext.RequestRollback();
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
        }

        /// <summary>
        /// Returns a task that invokes <see cref="M:System.Data.Common.DbCommand.ExecuteReaderAsync"/> on the <see cref="DbCommand"/> within a transaction,
        /// awaits <paramref name="mapRowAsync"/> to map each record of the <see cref="DbDataReader"/> to an instance of <typeparamref name="TOut"/>,
        /// and awaits the <paramref name="processDataObjectAsync"/> task on each mapped row.
        /// </summary>
        protected internal override async Task ExecuteReaderAsync<TOut>(Func<DbDataReader, Task<TOut>> mapRowAsync, Func<TOut, Task> processDataObjectAsync)
        {
            ParamCheck.Assert.IsNotNull(mapRowAsync, "mapRowAsync");
            ParamCheck.Assert.IsNotNull(processDataObjectAsync, "processDataObjectAsync");

            try
            {
                _Command.Connection = _TransactionContext.Connection;
                _Command.Transaction = _TransactionContext.Transaction;
                SafePreExecute(_Command);

                // execute and iterate the reader, map each row and process the results;
                // when done, the connection is closed
                DbDataReader reader;
                await using ((reader = await _Command.ExecuteReaderAsync()).ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        TOut dto = await mapRowAsync(reader).ConfigureAwait(false);
                        await processDataObjectAsync(dto).ConfigureAwait(false);
                    }
                }

                SafePostExecute(_Command);
                ContextLog.Q.CommandExecuted(_Command);
            }
            catch (Exception cmdEx)
            {
                _TransactionContext.RequestRollback();
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
        }

        #endregion

        #region Execute a Reader and Process First Row

        // note: to resolve compiler issues:
        // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/unconstrained-type-parameter-annotations#default-constraint
        // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/unconstrained-type-parameter-annotations#default-constraint

        /// <summary>
        /// Invokes <see cref="M:System.Data.Common.DbCommand.ExecuteReader"/> on the <see cref="DbCommand"/> within a transaction,
        /// invokes <paramref name="mapRow"/> to map the first record of the <see cref="DbDataReader"/> to an instance of <typeparamref name="TOut"/>,
        /// and then invokes the action <paramref name="processDataObject"/> on the mapped row. All other rows are ignored.
        /// </summary>
        protected internal override void ExecuteScalarReader<TOut>(Func<DbDataReader, TOut?> mapRow, Action<TOut?> processDataObject)
             where TOut : default
        {
            ParamCheck.Assert.IsNotNull(mapRow, "mapRow");
            ParamCheck.Assert.IsNotNull(processDataObject, "processDataObject");
            TOut? dto = default;

            try
            {
                _Command.Connection = _TransactionContext.Connection;
                _Command.Transaction = _TransactionContext.Transaction;
                SafePreExecute(_Command);

                // execute reader and map first record; when done, the connection is closed;
                using (DbDataReader reader = _Command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        dto = mapRow(reader);
                    }
                }

                SafePostExecute(_Command);
                ContextLog.Q.CommandExecuted(_Command);
            }
            catch (Exception cmdEx)
            {
                _TransactionContext.RequestRollback();
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
            // process the mapped row
            try
            {
                processDataObject(dto!);
            }
            catch (Exception processEx)
            {
                ContextLog.Q.TraceError(processEx, null);
                throw;
            }
        }

        /// <summary>
        /// Returns a task that invokes <see cref="M:System.Data.Common.DbCommand.ExecuteReaderAsync"/> on the <see cref="DbCommand"/> within a transaction,
        /// invokes <paramref name="mapRow"/> to map the first record of the <see cref="DbDataReader"/> to an instance of <typeparamref name="TOut"/>,
        /// and then invokes the action <paramref name="processDataObject"/> on the mapped row. All other rows are ignored.
        /// </summary>
        protected internal override async Task ExecuteScalarReaderAsync<TOut>(Func<DbDataReader, TOut?> mapRow, Action<TOut?> processDataObject)
             where TOut : default
        {
            ParamCheck.Assert.IsNotNull(mapRow, "mapRow");
            ParamCheck.Assert.IsNotNull(processDataObject, "processDataObject");
            TOut? dto = default;

            try
            {
                _Command.Connection = _TransactionContext.Connection;
                _Command.Transaction = _TransactionContext.Transaction;
                SafePreExecute(_Command);

                // execute reader and map first record; when done, the connection is closed;
                DbDataReader reader;
                await using ((reader = await _Command.ExecuteReaderAsync()).ConfigureAwait(false))
                {
                    if (reader.Read())
                    {
                        dto = mapRow(reader);
                    }
                }

                SafePostExecute(_Command);
                ContextLog.Q.CommandExecuted(_Command);
            }
            catch (Exception cmdEx)
            {
                _TransactionContext.RequestRollback();
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
            // process the mapped row
            try
            {
                processDataObject(dto);
            }
            catch (Exception processEx)
            {
                ContextLog.Q.TraceError(processEx, null);
                throw;
            }
        }

        /// <summary>
        /// Returns a task that invokes <see cref="M:System.Data.Common.DbCommand.ExecuteReaderAsync"/> on the <see cref="DbCommand"/> within a transaction,
        /// invokes <paramref name="mapRow"/> to map the first record of the <see cref="DbDataReader"/> to an instance of <typeparamref name="TOut"/>,
        /// and awaits the <paramref name="processDataObjectAsync"/> task on the mapped row. All other rows are ignored.
        /// </summary>
        protected internal override async Task ExecuteScalarReaderAsync<TOut>(Func<DbDataReader, TOut?> mapRow, Func<TOut?, Task> processDataObjectAsync)
             where TOut : default
        {
            ParamCheck.Assert.IsNotNull(mapRow, "mapRow");
            ParamCheck.Assert.IsNotNull(processDataObjectAsync, "processDataObjectAsync");
            TOut? dto = default;

            try
            {
                _Command.Connection = _TransactionContext.Connection;
                _Command.Transaction = _TransactionContext.Transaction;
                SafePreExecute(_Command);

                // execute reader and map first record; when done, the connection is closed;
                DbDataReader reader;
                await using ((reader = await _Command.ExecuteReaderAsync()).ConfigureAwait(false))
                {
                    if (reader.Read())
                    {
                        dto = mapRow(reader);
                    }
                }

                SafePostExecute(_Command);
                ContextLog.Q.CommandExecuted(_Command);
            }
            catch (Exception cmdEx)
            {
                _TransactionContext.RequestRollback();
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
            // process the mapped row
            try
            {
                await processDataObjectAsync(dto).ConfigureAwait(false);
            }
            catch (Exception processEx)
            {
                ContextLog.Q.TraceError(processEx, null);
                throw;
            }
        }

        /// <summary>
        /// Returns a task that invokes <see cref="M:System.Data.Common.DbCommand.ExecuteReaderAsync"/> on the <see cref="DbCommand"/> within a transaction,
        /// awaits <paramref name="mapRowAsync"/> to map the first record of the <see cref="DbDataReader"/> to an instance of <typeparamref name="TOut"/>,
        /// and then invokes the action <paramref name="processDataObject"/> on the mapped row. All other rows are ignored.
        /// </summary>
        protected internal override async Task ExecuteScalarReaderAsync<TOut>(Func<DbDataReader, Task<TOut?>> mapRowAsync, Action<TOut?> processDataObject)
             where TOut : default
        {
            ParamCheck.Assert.IsNotNull(mapRowAsync, "mapRowAsync");
            ParamCheck.Assert.IsNotNull(processDataObject, "processDataObject");
            TOut? dto = default;

            try
            {
                _Command.Connection = _TransactionContext.Connection;
                _Command.Transaction = _TransactionContext.Transaction;
                SafePreExecute(_Command);

                // execute reader and map first record; when done, the connection is closed;
                DbDataReader reader;
                await using ((reader = await _Command.ExecuteReaderAsync()).ConfigureAwait(false))
                {
                    if (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        dto = await mapRowAsync(reader).ConfigureAwait(false);
                    }
                }

                SafePostExecute(_Command);
                ContextLog.Q.CommandExecuted(_Command);
            }
            catch (Exception cmdEx)
            {
                _TransactionContext.RequestRollback();
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
            // process the mapped row
            try
            {
                processDataObject(dto);
            }
            catch (Exception processEx)
            {
                ContextLog.Q.TraceError(processEx, null);
                throw;
            }
        }

        /// <summary>
        /// Returns a task that invokes <see cref="M:System.Data.Common.DbCommand.ExecuteReaderAsync"/> on the <see cref="DbCommand"/> within a transaction,
        /// awaits <paramref name="mapRowAsync"/> to map the first record of the <see cref="DbDataReader"/> to an instance of <typeparamref name="TOut"/>,
        /// and awaits the <paramref name="processDataObjectAsync"/> task on the mapped row. All other rows are ignored.
        /// </summary>
        protected internal override async Task ExecuteScalarReaderAsync<TOut>(Func<DbDataReader, Task<TOut?>> mapRowAsync, Func<TOut?, Task> processDataObjectAsync)
             where TOut : default
        {
            ParamCheck.Assert.IsNotNull(mapRowAsync, "mapRowAsync");
            ParamCheck.Assert.IsNotNull(processDataObjectAsync, "processDataObjectAsync");
            TOut? dto = default;

            try
            {
                _Command.Connection = _TransactionContext.Connection;
                _Command.Transaction = _TransactionContext.Transaction;
                SafePreExecute(_Command);

                // execute reader and map first record; when done, the connection is closed;
                DbDataReader reader;
                await using ((reader = await _Command.ExecuteReaderAsync()).ConfigureAwait(false))
                {
                    if (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        dto = await mapRowAsync(reader).ConfigureAwait(false);
                    }
                }

                SafePostExecute(_Command);
                ContextLog.Q.CommandExecuted(_Command);
            }
            catch (Exception cmdEx)
            {
                _TransactionContext.RequestRollback();
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
            // process the mapped row
            try
            {
                await processDataObjectAsync(dto).ConfigureAwait(false);
            }
            catch (Exception processEx)
            {
                ContextLog.Q.TraceError(processEx, null);
                throw;
            }
        }
        #endregion

        #region Execute a Reader and Return First Row

        /// <summary>
        /// Invokes <see cref="M:System.Data.Common.DbCommand.ExecuteReader"/> on the <see cref="DbCommand"/> within a transaction,
        /// invokes <paramref name="mapRow"/> to map and return the first record of the <see cref="DbDataReader"/>
        /// as an instance of <typeparamref name="TOut"/>. All other rows are ignored.
        /// </summary>
        protected internal override TOut? ExecuteScalarReader<TOut>(Func<DbDataReader, TOut?> mapRow)
            where TOut : default
        {
            ParamCheck.Assert.IsNotNull(mapRow, "mapRow");
            TOut? result = default;

            try
            {
                _Command.Connection = _TransactionContext.Connection;
                _Command.Transaction = _TransactionContext.Transaction;
                SafePreExecute(_Command);

                // execute and read first record, map the row and return the result;
                // when done, the connection is closed
                using (DbDataReader reader = _Command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result = mapRow(reader);
                    }
                }

                SafePostExecute(_Command);
                ContextLog.Q.CommandExecuted(_Command);
                return result;
            }
            catch (Exception cmdEx)
            {
                _TransactionContext.RequestRollback();
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
        }

        /// <summary>
        /// Returns a task that invokes <see cref="M:System.Data.Common.DbCommand.ExecuteReaderAsync"/> on the <see cref="DbCommand"/> within a transaction,
        /// invokes <paramref name="mapRow"/> to map and return the first record of the <see cref="DbDataReader"/>
        /// as an instance of <typeparamref name="TOut"/> All other rows are ignored.
        /// </summary>
        protected internal override async Task<TOut?> ExecuteScalarReaderAsync<TOut>(Func<DbDataReader, TOut?> mapRow)
            where TOut : default
        {
            ParamCheck.Assert.IsNotNull(mapRow, "mapRow");
            TOut? result = default;

            try
            {
                _Command.Connection = _TransactionContext.Connection;
                _Command.Transaction = _TransactionContext.Transaction;
                SafePreExecute(_Command);

                // execute and read first record, map the row and return the result;
                // when done, the connection is closed
                DbDataReader reader;
                await using ((reader = await _Command.ExecuteReaderAsync()).ConfigureAwait(false))
                {
                    if (reader.Read())
                    {
                        result = mapRow(reader);
                    }
                }

                SafePostExecute(_Command);
                ContextLog.Q.CommandExecuted(_Command);
                return result;
            }
            catch (Exception cmdEx)
            {
                _TransactionContext.RequestRollback();
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
        }

        /// <summary>
        /// Returns a task that invokes <see cref="M:System.Data.Common.DbCommand.ExecuteReaderAsync"/> on the <see cref="DbCommand"/> within a transaction,
        /// invokes <paramref name="mapRowAsync"/> to map and return the first record of the <see cref="DbDataReader"/>
        /// as an instance of <typeparamref name="TOut"/> All other rows are ignored.
        /// </summary>
        protected internal override async Task<TOut?> ExecuteScalarReaderAsync<TOut>(Func<DbDataReader, Task<TOut?>> mapRowAsync)
            where TOut : default
        {
            ParamCheck.Assert.IsNotNull(mapRowAsync, "mapRowAsync");
            TOut? result = default;

            try
            {
                _Command.Connection = _TransactionContext.Connection;
                _Command.Transaction = _TransactionContext.Transaction;
                SafePreExecute(_Command);

                // execute and read first record, map the row and return the result;
                // when done, the connection is closed
                DbDataReader reader;
                await using ((reader = await _Command.ExecuteReaderAsync()).ConfigureAwait(false))
                {
                    if (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        result = await mapRowAsync(reader).ConfigureAwait(false);
                    }
                }

                SafePostExecute(_Command);
                ContextLog.Q.CommandExecuted(_Command);
                return result;
            }
            catch (Exception cmdEx)
            {
                _TransactionContext.RequestRollback();
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
        }

        #endregion


        ///// <summary>
        ///// Invokes <see cref="M:System.Data.Common.DbCommand.ExecuteNonQuery"/> on the <see cref="DbCommand"/> 
        ///// within a transaction and returns the number of rows affected.
        ///// </summary>
        ///// <returns>The number of rows affected.</returns>
        //public int BulkInsert<T>(IEnumerable<T> items, Func<T, DbCommand, DbCommand> prepareCommand)
        //{
        //    int counter = 0;
        //    try
        //    {
        //        _Command.Connection = _TransactionContext.Connection;
        //        _Command.Transaction = _TransactionContext.Transaction;
        //        SafePreExecute(_Command);
        //        foreach (T input in items)
        //        {
        //            var insertCmd = prepareCommand(input, _Command);
        //            insertCmd.ExecuteNonQuery();
        //            counter++;
        //        }
        //        SafePostExecute(_Command);
        //        ContextLog.Q.CommandExecuted(_Command);
        //        return counter;
        //    }
        //    catch (Exception cmdEx)
        //    {
        //        _TransactionContext.RequestRollback();
        //        ContextLog.Q.CommandFailed(_Command, cmdEx);
        //        throw;
        //    }
        //}


        #region IDisposable

        /// <summary>Releases the unmanaged resources and optionally releases the managed resources.</summary>
        /// <param name="isManagedCall">true to release both managed and unmanaged resources; false to release only unmanaged resources. </param>
        protected override void Dispose(bool isManagedCall)
        {
            // allow the AdoTransactionContext to handle dispose semantics
        }

        /// <summary>Releases the unmanaged resources and optionally releases the managed resources.</summary>
        /// <param name="isManagedCall">true to release both managed and unmanaged resources; false to release only unmanaged resources. </param>
        protected override async ValueTask DisposeAsync(bool isManagedCall)
        {
            // allow the AdoTransactionContext to handle dispose semantics
            await Task.CompletedTask;
        }

        #endregion
    }
}
