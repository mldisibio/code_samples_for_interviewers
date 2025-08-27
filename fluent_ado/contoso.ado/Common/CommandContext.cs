using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using System.Xml;
using contoso.ado.EventListeners;
using contoso.ado.Fluent;
using contoso.ado.Internals;

namespace contoso.ado.Common
{
    /// <summary>
    /// Default implementation for the context wrapping the execution of a <see cref="DbCommand"/>
    /// common to the Ado.Net framework.
    /// The context is constrained to its connection lifetime.
    /// The connection is opened and closed per command without any external transaction management.
    /// Other provider specific Command implementations can inherit from this implementation and need only
    /// override provider specific functionality.
    /// </summary>
    public class CommandContext : IDisposable, IAsyncDisposable
    {
        readonly static Action<DbCommand> _cmdNoOp = _ => { };
        bool _cmdAlreadyDisposed;
        List<Action<DbCommand>>? _preExecuteQueue;
        List<Action<DbCommand>>? _postExecuteQueue;

        /// <summary>True if 'Dispose' has already been called on this instance.</summary>
        protected bool _IsAlreadyDisposed;

        /// <summary>The <see cref="DbCommand"/> to be executed by this instance.</summary>
        readonly protected DbCommand _Command;

        /// <summary>The <see cref="Database"/> context for this instance.</summary>
        readonly protected Database _DbContext;

        /// <summary>Instance of the factory class facilitating creation of an unconfigured <see cref="DbParameter"/>.</summary>
        readonly protected ParameterContext _CmdParameterFactory;

        /// <summary>Instance of the factory class facilitating creation of an xml <see cref="DbParameter"/>.</summary>
        readonly protected XmlParameterContext _XmlParameterFactory;

        /// <summary>Initialize with the given <see cref="Database"/> and <see cref="DbCommand"/>.</summary>
        protected internal CommandContext(DbCommand cmd, Database db)
        {
            _DbContext = db.ThrowIfNull();
            _Command = cmd.ThrowIfNull();
            _Command.Disposed += (s, e) => _cmdAlreadyDisposed = true;
            _CmdParameterFactory = new ParameterContext(_DbContext);
            _XmlParameterFactory = new XmlParameterContext(_DbContext);
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

        /// <summary>Add the already configured <see cref="DbParameter"/>s to the <see cref="DbCommand"/> in context.</summary>
        public CommandContext WithParameter(DbParameter dbParameter)
        {
            ParamCheck.Assert.IsNotNull(dbParameter, "dbParameter");
            AddOrReplaceParameters(dbParameter);
            return this;
        }

        /// <summary>Add the already configured <see cref="DbParameter"/> to the <see cref="DbCommand"/> in context and ensures its Directory is set to 'Output'.</summary>
        public CommandContext WithOutputParameter(DbParameter dbParameter)
        {
            ParamCheck.Assert.IsNotNull(dbParameter, "dbParameter");
            dbParameter.Direction = ParameterDirection.Output;
            AddOrReplaceParameters(dbParameter);
            return this;
        }

        /// <summary>
        /// Applies the given delegate to create a single <see cref="DbParameter"/> using a <see cref="ParameterContext"/>
        /// and adds the parameter to the <see cref="DbCommand"/> in context.
        /// </summary>
        public CommandContext WithParameter(Func<ParameterContext, DbParameter> createParameter)
        {
            ParamCheck.Assert.IsNotNull(createParameter, "createParameter delegate");
            DbParameter dbParam = createParameter(_CmdParameterFactory);
            AddOrReplaceParameters(dbParam);
            return this;
        }

        /// <summary>
        /// Applies the given delegate to create a collection of <see cref="DbParameter"/> using a <see cref="ParameterContext"/>
        /// and adds the parameters to the <see cref="DbCommand"/> in context.
        /// </summary>
        public CommandContext WithParameters(Func<ParameterContext, DbParameter[]> createParameters)
        {
            ParamCheck.Assert.IsNotNull(createParameters, "createParameters delegate");
            DbParameter[] dbParams = createParameters(_CmdParameterFactory);
            AddOrReplaceParameters(dbParams);
            return this;
        }

        /// <summary>
        /// Applies the given delegate to create a collection of <see cref="DbParameter"/> using a <see cref="ParameterContext"/>
        /// and adds the parameters to the <see cref="DbCommand"/> in context.
        /// The delegate accepts a parameter source, which might be a data request object requiring interpretation or processing
        /// for conversion to a set of parameters.
        /// </summary>
        public CommandContext WithParameters<TSrc>(TSrc paramSource, Func<ParameterContext, TSrc, DbParameter[]> createParameters)
        {
            ParamCheck.Assert.IsNotNull(createParameters, "createParameters delegate");
            DbParameter[] dbParams = createParameters(_CmdParameterFactory, paramSource);
            AddOrReplaceParameters(dbParams);
            return this;
        }

        /// <summary>
        /// Applies the given delegate to create a single <see cref="DbParameter"/> using a <see cref="XmlParameterContext"/>
        /// and adds the parameter to the <see cref="DbCommand"/> in context.
        /// </summary>
        public CommandContext WithXmlParameter(Func<XmlParameterContext, DbParameter> createXmlParameter)
        {
            ParamCheck.Assert.IsNotNull(createXmlParameter, "createXmlParameter delegate");
            DbParameter dbParam = createXmlParameter(_XmlParameterFactory);
            AddOrReplaceParameters(dbParam);
            return this;
        }

        /// <summary>
        /// Applies the given delegate to create a collection of <see cref="DbParameter"/> using a <see cref="XmlParameterContext"/>
        /// and adds the parameters to the <see cref="DbCommand"/> in context.
        /// </summary>
        public CommandContext WithXmlParameters(Func<XmlParameterContext, DbParameter[]> createXmlParameters)
        {
            ParamCheck.Assert.IsNotNull(createXmlParameters, "createXmlParameters delegate");
            DbParameter[] dbParams = createXmlParameters(_XmlParameterFactory);
            AddOrReplaceParameters(dbParams);
            return this;
        }

        /// <summary>
        /// Applies the given delegate to create a collection of <see cref="DbParameter"/> using a <see cref="XmlParameterContext"/>
        /// and adds the parameters to the <see cref="DbCommand"/> in context.
        /// The delegate accepts a parameter source, which might be a data request object requiring interpretation or processing
        /// for conversion to a set of parameters.
        /// </summary>
        public CommandContext WithXmlParameters<TSrc>(TSrc paramSource, Func<XmlParameterContext, TSrc, DbParameter[]> createXmlParameters)
        {
            ParamCheck.Assert.IsNotNull(createXmlParameters, "createXmlParameters delegate");
            DbParameter[] dbParams = createXmlParameters(_XmlParameterFactory, paramSource);
            AddOrReplaceParameters(dbParams);
            return this;
        }

        /// <summary>Add one or more <see cref="DbParameter"/>s to the <see cref="DbCommand"/> in context, or replace any pre-existing ones with the same name.</summary>
        protected void AddOrReplaceParameters(params DbParameter[] dbParameters)
        {
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


        #endregion

        #region Non-Reader Execution

        /// <summary>
        /// Invokes <see cref="M:System.Data.Common.DbCommand.ExecuteNonQuery"/> on the <see cref="DbCommand"/> in context,
        /// closes the connection, and returns the number of rows affected.
        /// </summary>
        /// <returns>The number of rows affected.</returns>
        public virtual int ExecuteNonQuery()
        {
            try
            {
                using var connection = _DbContext.CreateOpenedConnection();
                _Command.Connection = connection;
                SafePreExecute(_Command);
                int result = _Command.ExecuteNonQuery();
                SafePostExecute(_Command);
                ContextLog.Q.CommandExecuted(_Command);
                return result;
            }
            catch (Exception cmdEx)
            {
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
            finally
            {
                _Command.SafeDispose();
            }
        }

        /// <summary>
        /// Invokes <see cref="M:System.Data.Common.DbCommand.ExecuteNonQueryAsync"/> on the <see cref="DbCommand"/> in context,
        /// closes the connection, and returns the number of rows affected.
        /// </summary>
        /// <returns>The number of rows affected.</returns>
        public virtual async Task<int> ExecuteNonQueryAsync()
        {
            try
            {
                DbConnection connection;
                await using ((connection = await _DbContext.CreateOpenedConnectionAsync()).ConfigureAwait(false))
                {
                    _Command.Connection = connection;
                    SafePreExecute(_Command);
                    int result = await _Command.ExecuteNonQueryAsync().ConfigureAwait(false);
                    SafePostExecute(_Command);
                    ContextLog.Q.CommandExecuted(_Command);
                    return result;
                }
            }
            catch (Exception cmdEx)
            {
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
            finally
            {
                _Command.SafeDispose();
            }
        }

        /// <summary>
        /// Invokes <see cref="M:System.Data.Common.DbCommand.ExecuteScalar"/> on the <see cref="DbCommand"/> in context,
        /// closes the connection, and returns the first column of the first row in the last result set returned by the query.
        /// Extra columns or rows are ignored.
        /// </summary>
        /// <returns>The first column of the first row in the last result set, or a <see langword="null"/> reference.</returns>
        public virtual T? ExecuteScalar<T>()
        {
            object? scalarResult = null;
            try
            {
                using (var connection = _DbContext.CreateOpenedConnection())
                {
                    _Command.Connection = connection;
                    SafePreExecute(_Command);

                    // execute the command asynchronously using ExecuteReader instead of ExecuteScalar
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
                }
                ContextLog.Q.CommandExecuted(_Command);
                return scalarResult == null ? default : (T?)scalarResult;
            }
            catch (Exception cmdEx)
            {
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
            finally
            {
                _Command.SafeDispose();
            }
        }

        /// <summary>
        /// Invokes <see cref="M:System.Data.Common.DbCommand.ExecuteScalarAsync"/> on the <see cref="DbCommand"/> in context,
        /// closes the connection, and returns the first column of the first row in the last result set returned by the query.
        /// Extra columns or rows are ignored.
        /// </summary>
        /// <returns>The first column of the first row in the last result set, or a <see langword="null"/> reference.</returns>
        public virtual async Task<T?> ExecuteScalarAsync<T>()
        {
            object? scalarResult = null;
            try
            {
                DbConnection connection;
                await using ((connection = await _DbContext.CreateOpenedConnectionAsync()).ConfigureAwait(false))
                {
                    _Command.Connection = connection;
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
                }
                ContextLog.Q.CommandExecuted(_Command);
                return scalarResult == null ? default : (T)scalarResult;
            }
            catch (Exception cmdEx)
            {
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
            finally
            {
                _Command.SafeDispose();
            }
        }

        #endregion

        #region Mapping the Data Reader to DTO

        /// <summary>
        /// Uses the delegate <paramref name="mapRow"/> for mapping each <see cref="DbDataReader"/> record returned by the call to <see cref="M:System.Data.Common.DbCommand.ExecuteReader"/>
        /// to an instance of <typeparamref name="TOut"/>.
        /// </summary>
        public ReaderMapContext<TOut?> WithMap<TOut>(Func<DbDataReader, TOut?> mapRow)
        {
            ParamCheck.Assert.IsNotNull(mapRow, "mapRow");
            return new ReaderMapContext<TOut?>(mapRow, this);
        }

        /// <summary>
        /// Uses the delegate <paramref name="mapRowAsync"/> for mapping each <see cref="DbDataReader"/> record returned by the call to <see cref="M:System.Data.Common.DbCommand.ExecuteReader"/>
        /// to an instance of <typeparamref name="TOut"/>.
        /// </summary>
        public ReaderMapTaskContext<TOut?> WithMapTask<TOut>(Func<DbDataReader, Task<TOut?>> mapRowAsync)
        {
            ParamCheck.Assert.IsNotNull(mapRowAsync, "mapRowAsync");
            return new ReaderMapTaskContext<TOut?>(mapRowAsync, this);
        }

        #endregion

        #region Mapping the Xml Reader to DTO

        /// <summary>
        /// Uses the delegate <paramref name="mapXml"/> as the source for parsing the content returned from execution of an a <see cref="DbCommand"/>
        /// yielding an <see cref="XmlReader"/> to <typeparamref name="TOut"/>.
        /// </summary>
        public XmlReaderMapContext<TOut> WithXmlMap<TOut>(Func<XmlReader, TOut> mapXml)
        {
            ParamCheck.Assert.IsNotNull(mapXml, "mapXml");
            return new XmlReaderMapContext<TOut>(mapXml, this);
        }

        /// <summary>
        /// Uses the delegate <paramref name="mapXmlAsync"/> as the delegate for parsing the content returned from execution of an a <see cref="DbCommand"/>
        /// yielding an <see cref="XmlReader"/> to <typeparamref name="TOut"/>.
        /// </summary>
        public XmlReaderMapTaskContext<TOut> WithXmlMapTask<TOut>(Func<XmlReader, Task<TOut>> mapXmlAsync)
        {
            ParamCheck.Assert.IsNotNull(mapXmlAsync, "mapXmlAsync");
            return new XmlReaderMapTaskContext<TOut>(mapXmlAsync, this);
        }

        #endregion

        #region Execute a Reader and Process Results

        /// <summary>
        /// Invokes <see cref="M:System.Data.Common.DbCommand.ExecuteReader"/> on the <see cref="DbCommand"/> in context,
        /// invokes <paramref name="mapRow"/> to map each record of the <see cref="DbDataReader"/> to an instance of <typeparamref name="TOut"/>,
        /// and then invokes the action <paramref name="processDataObject"/> on each mapped row.
        /// </summary>
        protected internal virtual void ExecuteReader<TOut>(Func<DbDataReader, TOut> mapRow, Action<TOut> processDataObject)
        {
            ParamCheck.Assert.IsNotNull(mapRow, "mapRow");
            ParamCheck.Assert.IsNotNull(processDataObject, "processDataObject");

            try
            {
                using (var connection = _DbContext.CreateOpenedConnection())
                {
                    _Command.Connection = connection;
                    SafePreExecute(_Command);

                    // execute and iterate the reader, map each row and process the results;
                    // when done, the connection is closed
                    using DbDataReader reader = _Command.ExecuteReader();
                    while (reader.Read())
                    {
                        TOut dto = mapRow(reader);
                        processDataObject(dto);
                    }

                    SafePostExecute(_Command);
                }
                ContextLog.Q.CommandExecuted(_Command);
            }
            catch (Exception cmdEx)
            {
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
            finally
            {
                _Command.SafeDispose();
            }
        }

        /// <summary>
        /// Returns a task that invokes <see cref="M:System.Data.Common.DbCommand.ExecuteReaderAsync"/> on the <see cref="DbCommand"/> in context,
        /// invokes <paramref name="mapRow"/> to map each record of the <see cref="DbDataReader"/> to an instance of <typeparamref name="TOut"/>,
        /// and then invokes the action <paramref name="processDataObject"/> on each mapped row.
        /// </summary>
        protected internal virtual async Task ExecuteReaderAsync<TOut>(Func<DbDataReader, TOut> mapRow, Action<TOut> processDataObject)
        {
            ParamCheck.Assert.IsNotNull(mapRow, "mapRow");
            ParamCheck.Assert.IsNotNull(processDataObject, "processDataObject");

            try
            {
                DbConnection connection;
                await using ((connection = await _DbContext.CreateOpenedConnectionAsync()).ConfigureAwait(false))
                {
                    _Command.Connection = connection;
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
                }
                ContextLog.Q.CommandExecuted(_Command);
            }
            catch (Exception cmdEx)
            {
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
            finally
            {
                _Command.SafeDispose();
            }
        }

        /// <summary>
        /// Returns a task that invokes <see cref="M:System.Data.Common.DbCommand.ExecuteReaderAsync"/> on the <see cref="DbCommand"/> in context,
        /// invokes <paramref name="mapRow"/> to map each record of the <see cref="DbDataReader"/> to an instance of <typeparamref name="TOut"/>,
        /// and awaits the <paramref name="processDataObjectAsync"/> task on each mapped row.
        /// </summary>
        protected internal virtual async Task ExecuteReaderAsync<TOut>(Func<DbDataReader, TOut> mapRow, Func<TOut, Task> processDataObjectAsync)
        {
            ParamCheck.Assert.IsNotNull(mapRow, "mapRow");
            ParamCheck.Assert.IsNotNull(processDataObjectAsync, "processDataObjectAsync");

            try
            {
                DbConnection connection;
                await using ((connection = await _DbContext.CreateOpenedConnectionAsync()).ConfigureAwait(false))
                {
                    _Command.Connection = connection;
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
                }
                ContextLog.Q.CommandExecuted(_Command);
            }
            catch (Exception cmdEx)
            {
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
            finally
            {
                _Command.SafeDispose();
            }
        }

        /// <summary>
        /// Returns a task that invokes <see cref="M:System.Data.Common.DbCommand.ExecuteReaderAsync"/> on the <see cref="DbCommand"/> in context,
        /// awaits <paramref name="mapRowAsync"/> to map each record of the <see cref="DbDataReader"/> to an instance of <typeparamref name="TOut"/>,
        /// and invokes the action <paramref name="processDataObject"/> on each mapped row.
        /// </summary>
        protected internal virtual async Task ExecuteReaderAsync<TOut>(Func<DbDataReader, Task<TOut>> mapRowAsync, Action<TOut> processDataObject)
        {
            ParamCheck.Assert.IsNotNull(mapRowAsync, "mapRowAsync");
            ParamCheck.Assert.IsNotNull(processDataObject, "processDataObject");

            try
            {
                DbConnection connection;
                await using ((connection = await _DbContext.CreateOpenedConnectionAsync()).ConfigureAwait(false))
                {
                    _Command.Connection = connection;
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
                }
                ContextLog.Q.CommandExecuted(_Command);
            }
            catch (Exception cmdEx)
            {
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
            finally
            {
                _Command.SafeDispose();
            }
        }

        /// <summary>
        /// Returns a task that invokes <see cref="M:System.Data.Common.DbCommand.ExecuteReaderAsync"/> on the <see cref="DbCommand"/> in context,
        /// awaits <paramref name="mapRowAsync"/> to map each record of the <see cref="DbDataReader"/> to an instance of <typeparamref name="TOut"/>,
        /// and awaits the <paramref name="processDataObjectAsync"/> task on each mapped row.
        /// </summary>
        protected internal virtual async Task ExecuteReaderAsync<TOut>(Func<DbDataReader, Task<TOut>> mapRowAsync, Func<TOut, Task> processDataObjectAsync)
        {
            ParamCheck.Assert.IsNotNull(mapRowAsync, "mapRowAsync");
            ParamCheck.Assert.IsNotNull(processDataObjectAsync, "processDataObjectAsync");

            try
            {
                DbConnection connection;
                await using ((connection = await _DbContext.CreateOpenedConnectionAsync()).ConfigureAwait(false))
                {
                    _Command.Connection = connection;
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
                }
                ContextLog.Q.CommandExecuted(_Command);
            }
            catch (Exception cmdEx)
            {
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
            finally
            {
                _Command.SafeDispose();
            }
        }

        #endregion

        #region Execute a Reader and Process First Row

        /// <summary>
        /// Invokes <see cref="M:System.Data.Common.DbCommand.ExecuteReader"/> on the <see cref="DbCommand"/> in context,
        /// invokes <paramref name="mapRow"/> to map the first record of the <see cref="DbDataReader"/> to an instance of <typeparamref name="TOut"/>,
        /// and then invokes the action <paramref name="processDataObject"/> on the mapped row. All other rows are ignored.
        /// </summary>
        protected internal virtual void ExecuteScalarReader<TOut>(Func<DbDataReader, TOut?> mapRow, Action<TOut?> processDataObject)
        {
            ParamCheck.Assert.IsNotNull(mapRow, "mapRow");
            ParamCheck.Assert.IsNotNull(processDataObject, "processDataObject");
            TOut? dto = default;

            try
            {
                using (var connection = _DbContext.CreateOpenedConnection())
                {
                    _Command.Connection = connection;
                    SafePreExecute(_Command);

                    // execute reader and map first record; when done, the connection is closed;
                    using DbDataReader reader = _Command.ExecuteReader();
                    if (reader.Read())
                    {
                        dto = mapRow(reader);
                    }

                    SafePostExecute(_Command);
                }
                ContextLog.Q.CommandExecuted(_Command);
            }
            catch (Exception cmdEx)
            {
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
            finally
            {
                _Command.SafeDispose();
            }
            // process the mapped row
            try
            {
                processDataObject(dto);
            }
            catch (Exception processEx)
            {
                ContextLog.Q.TraceError(processEx);
                throw;
            }
        }

        /// <summary>
        /// Returns a task that invokes <see cref="M:System.Data.Common.DbCommand.ExecuteReaderAsync"/> on the <see cref="DbCommand"/> in context,
        /// invokes <paramref name="mapRow"/> to map the first record of the <see cref="DbDataReader"/> to an instance of <typeparamref name="TOut"/>,
        /// and then invokes the action <paramref name="processDataObject"/> on the mapped row. All other rows are ignored.
        /// </summary>
        protected internal virtual async Task ExecuteScalarReaderAsync<TOut>(Func<DbDataReader, TOut?> mapRow, Action<TOut?> processDataObject)
        {
            ParamCheck.Assert.IsNotNull(mapRow, "mapRow");
            ParamCheck.Assert.IsNotNull(processDataObject, "processDataObject");
            TOut? dto = default;

            try
            {
                DbConnection connection;
                await using ((connection = await _DbContext.CreateOpenedConnectionAsync()).ConfigureAwait(false))
                {
                    _Command.Connection = connection;
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
                }
                ContextLog.Q.CommandExecuted(_Command);
            }
            catch (Exception cmdEx)
            {
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
            finally
            {
                _Command.SafeDispose();
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
        /// Returns a task that invokes <see cref="M:System.Data.Common.DbCommand.ExecuteReaderAsync"/> on the <see cref="DbCommand"/> in context,
        /// invokes <paramref name="mapRow"/> to map the first record of the <see cref="DbDataReader"/> to an instance of <typeparamref name="TOut"/>,
        /// and awaits the <paramref name="processDataObjectAsync"/> task on the mapped row. All other rows are ignored.
        /// </summary>
        protected internal virtual async Task ExecuteScalarReaderAsync<TOut>(Func<DbDataReader, TOut?> mapRow, Func<TOut?, Task> processDataObjectAsync)
        {
            ParamCheck.Assert.IsNotNull(mapRow, "mapRow");
            ParamCheck.Assert.IsNotNull(processDataObjectAsync, "processDataObjectAsync");
            TOut? dto = default;

            try
            {
                DbConnection connection;
                await using ((connection = await _DbContext.CreateOpenedConnectionAsync()).ConfigureAwait(false))
                {
                    _Command.Connection = connection;
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
                }
                ContextLog.Q.CommandExecuted(_Command);
            }
            catch (Exception cmdEx)
            {
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
            finally
            {
                _Command.SafeDispose();
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
        /// Returns a task that invokes <see cref="M:System.Data.Common.DbCommand.ExecuteReaderAsync"/> on the <see cref="DbCommand"/> in context,
        /// awaits <paramref name="mapRowAsync"/> to map the first record of the <see cref="DbDataReader"/> to an instance of <typeparamref name="TOut"/>,
        /// and then invokes the action <paramref name="processDataObject"/> on the mapped row. All other rows are ignored.
        /// </summary>
        protected internal virtual async Task ExecuteScalarReaderAsync<TOut>(Func<DbDataReader, Task<TOut?>> mapRowAsync, Action<TOut?> processDataObject)
        {
            ParamCheck.Assert.IsNotNull(mapRowAsync, "mapRowAsync");
            ParamCheck.Assert.IsNotNull(processDataObject, "processDataObject");
            TOut? dto = default;

            try
            {
                DbConnection connection;
                await using ((connection = await _DbContext.CreateOpenedConnectionAsync()).ConfigureAwait(false))
                {
                    _Command.Connection = connection;
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
                }
                ContextLog.Q.CommandExecuted(_Command);
            }
            catch (Exception cmdEx)
            {
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
            finally
            {
                _Command.SafeDispose();
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
        /// Returns a task that invokes <see cref="M:System.Data.Common.DbCommand.ExecuteReaderAsync"/> on the <see cref="DbCommand"/> in context,
        /// awaits <paramref name="mapRowAsync"/> to map the first record of the <see cref="DbDataReader"/> to an instance of <typeparamref name="TOut"/>,
        /// and awaits the <paramref name="processDataObjectAsync"/> task on the mapped row. All other rows are ignored.
        /// </summary>
        protected internal virtual async Task ExecuteScalarReaderAsync<TOut>(Func<DbDataReader, Task<TOut?>> mapRowAsync, Func<TOut?, Task> processDataObjectAsync)
        {
            ParamCheck.Assert.IsNotNull(mapRowAsync, "mapRowAsync");
            ParamCheck.Assert.IsNotNull(processDataObjectAsync, "processDataObjectAsync");
            TOut? dto = default;

            try
            {
                DbConnection connection;
                await using ((connection = await _DbContext.CreateOpenedConnectionAsync()).ConfigureAwait(false))
                {
                    _Command.Connection = connection;
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
                }
                ContextLog.Q.CommandExecuted(_Command);
            }
            catch (Exception cmdEx)
            {
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
            finally
            {
                _Command.SafeDispose();
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
        /// Invokes <see cref="M:System.Data.Common.DbCommand.ExecuteReader"/> on the <see cref="DbCommand"/> in context,
        /// invokes <paramref name="mapRow"/> to map and return the first record of the <see cref="DbDataReader"/>
        /// as an instance of <typeparamref name="TOut"/>. All other rows are ignored.
        /// </summary>
        protected internal virtual TOut? ExecuteScalarReader<TOut>(Func<DbDataReader, TOut?> mapRow)
        {
            ParamCheck.Assert.IsNotNull(mapRow, "mapRow");
            TOut? result = default;

            try
            {
                using (var connection = _DbContext.CreateOpenedConnection())
                {
                    _Command.Connection = connection;
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
                }
                ContextLog.Q.CommandExecuted(_Command);
                return result;
            }
            catch (Exception cmdEx)
            {
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
            finally
            {
                _Command.SafeDispose();
            }
        }

        /// <summary>
        /// Returns a task that invokes <see cref="M:System.Data.Common.DbCommand.ExecuteReaderAsync"/> on the <see cref="DbCommand"/> in context,
        /// invokes <paramref name="mapRow"/> to map and return the first record of the <see cref="DbDataReader"/>
        /// as an instance of <typeparamref name="TOut"/> All other rows are ignored.
        /// </summary>
        protected internal virtual async Task<TOut?> ExecuteScalarReaderAsync<TOut>(Func<DbDataReader, TOut?> mapRow)
        {
            ParamCheck.Assert.IsNotNull(mapRow, "mapRow");
            TOut? result = default;

            try
            {
                DbConnection connection;
                await using ((connection = await _DbContext.CreateOpenedConnectionAsync()).ConfigureAwait(false))
                {
                    _Command.Connection = connection;
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
                }
                ContextLog.Q.CommandExecuted(_Command);
                return result;
            }
            catch (Exception cmdEx)
            {
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
            finally
            {
                _Command.SafeDispose();
            }
        }

        /// <summary>
        /// Returns a task that invokes <see cref="M:System.Data.Common.DbCommand.ExecuteReaderAsync"/> on the <see cref="DbCommand"/> in context,
        /// invokes <paramref name="mapRowAsync"/> to map and return the first record of the <see cref="DbDataReader"/>
        /// as an instance of <typeparamref name="TOut"/> All other rows are ignored.
        /// </summary>
        protected internal virtual async Task<TOut?> ExecuteScalarReaderAsync<TOut>(Func<DbDataReader, Task<TOut?>> mapRowAsync)
        {
            ParamCheck.Assert.IsNotNull(mapRowAsync, "mapRowAsync");
            TOut? result = default;

            try
            {
                DbConnection connection;
                await using ((connection = await _DbContext.CreateOpenedConnectionAsync()).ConfigureAwait(false))
                {
                    _Command.Connection = connection;
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
                }
                ContextLog.Q.CommandExecuted(_Command);
                return result;
            }
            catch (Exception cmdEx)
            {
                ContextLog.Q.CommandFailed(_Command, cmdEx);
                throw;
            }
            finally
            {
                _Command.SafeDispose();
            }
        }

        #endregion

        #region Execute an Xml Reader and Process Result

        /// <summary>
        /// Invokes ExecuteXmlReader on the <see cref="DbCommand"/> in context if supported,
        /// invokes <paramref name="mapXml"/> to map the content of the <see cref="XmlReader"/> to an instance of <typeparamref name="TOut"/>,
        /// and then invokes the action <paramref name="processDataObject"/> on the mapped content.
        /// </summary>
        protected internal virtual void ExecuteXmlReader<TOut>(Func<XmlReader, TOut> mapXml, Action<TOut> processDataObject)
        {
            throw new InvalidOperationException("The DbCommand implementation does not support query execution that returns an XmlReader.");
        }

        /// <summary>
        /// Returns a task that invokes ExecuteXmlReader on the <see cref="DbCommand"/> in context if supported,
        /// invokes <paramref name="mapXml"/> to map the content of the <see cref="XmlReader"/> to an instance of <typeparamref name="TOut"/>,
        /// and then invokes the action <paramref name="processDataObject"/> on the mapped row. All other rows are ignored.
        /// </summary>
        protected internal virtual Task ExecuteXmlReaderAsync<TOut>(Func<XmlReader, TOut> mapXml, Action<TOut> processDataObject)
        {
            throw new InvalidOperationException("The DbCommand implementation does not support query execution that returns an XmlReader.");
        }

        /// <summary>
        /// Returns a task that invokes ExecuteXmlReader on the <see cref="DbCommand"/> in context if supported,
        /// invokes <paramref name="mapXml"/> to map the content of the <see cref="XmlReader"/> to an instance of <typeparamref name="TOut"/>,
        /// and awaits the <paramref name="processDataObjectAsync"/> task on the mapped row. All other rows are ignored.
        /// </summary>
        protected internal virtual Task ExecuteXmlReaderAsync<TOut>(Func<XmlReader, TOut> mapXml, Func<TOut, Task> processDataObjectAsync)
        {
            throw new InvalidOperationException("The DbCommand implementation does not support query execution that returns an XmlReader.");
        }

        /// <summary>
        /// Returns a task that invokes ExecuteXmlReader on the <see cref="DbCommand"/> in context if supported,
        /// awaits <paramref name="mapXmlAsync"/> to map the content of the <see cref="XmlReader"/> to an instance of <typeparamref name="TOut"/>,
        /// and then invokes the action <paramref name="processDataObject"/> on the mapped row. All other rows are ignored.
        /// </summary>
        protected internal virtual Task ExecuteXmlReaderAsync<TOut>(Func<XmlReader, Task<TOut>> mapXmlAsync, Action<TOut> processDataObject)
        {
            throw new InvalidOperationException("The DbCommand implementation does not support query execution that returns an XmlReader.");
        }

        /// <summary>
        /// Returns a task that invokes ExecuteXmlReader on the <see cref="DbCommand"/> in context if supported,
        /// awaits <paramref name="mapXmlAsync"/> to map the content of the <see cref="XmlReader"/> to an instance of <typeparamref name="TOut"/>,
        /// and awaits the <paramref name="processDataObjectAsync"/> task on the mapped row. All other rows are ignored.
        /// </summary>
        protected internal virtual Task ExecuteXmlReaderAsync<TOut>(Func<XmlReader, Task<TOut>> mapXmlAsync, Func<TOut, Task> processDataObjectAsync)
        {
            throw new InvalidOperationException("The DbCommand implementation does not support query execution that returns an XmlReader.");
        }

        #endregion

        #region Execute an Xml Reader and Return Result

        /// <summary>
        /// Invokes ExecuteXmlReader on the <see cref="DbCommand"/> in context if supported,
        /// invokes <paramref name="mapXml"/> to map and return the content of the <see cref="XmlReader"/>
        /// as an instance of <typeparamref name="TOut"/>.
        /// </summary>
        protected internal virtual TOut ExecuteXmlReader<TOut>(Func<XmlReader, TOut> mapXml)
        {
            throw new InvalidOperationException("The DbCommand implementation does not support query execution that returns an XmlReader.");
        }

        /// <summary>
        /// Returns a task that invokes ExecuteXmlReader on the <see cref="DbCommand"/> in context if supported,
        /// invokes <paramref name="mapXml"/> to map and return the content of the <see cref="XmlReader"/>
        /// as an instance of <typeparamref name="TOut"/>.
        /// </summary>
        protected internal virtual Task<TOut> ExecuteXmlReaderAsync<TOut>(Func<XmlReader, TOut> mapXml)
        {
            throw new InvalidOperationException("The DbCommand implementation does not support query execution that returns an XmlReader.");
        }

        /// <summary>
        /// Returns a task that invokes ExecuteXmlReader on the <see cref="DbCommand"/> in context if supported,
        /// and awaits <paramref name="mapXmlAsync"/> to map and return the content of the <see cref="XmlReader"/>
        /// as an instance of <typeparamref name="TOut"/>.
        /// </summary>
        protected internal virtual Task<TOut> ExecuteXmlReaderAsync<TOut>(Func<XmlReader, Task<TOut>> mapXmlAsync)
        {
            throw new InvalidOperationException("The DbCommand implementation does not support query execution that returns an XmlReader.");
        }

        #endregion

        #region IDisposable

        /// <summary>Releases all resources.</summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Releases the unmanaged resources and optionally releases the managed resources.</summary>
        /// <param name="isManagedCall">true to release both managed and unmanaged resources; false to release only unmanaged resources. </param>
        protected virtual void Dispose(bool isManagedCall)
        {
            if (!_IsAlreadyDisposed)
            {
                _IsAlreadyDisposed = true;
                if (isManagedCall)
                {
                    lock (this)
                    {
                        if (_Command != null && !_cmdAlreadyDisposed)
                        {
                            try { _Command.Dispose(); }
                            catch { }
                        }
                    }
                }
            }
        }

        /// <summary>Releases all resources.</summary>
        public ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return DisposeAsync(true);
        }

        /// <summary>Releases the unmanaged resources and optionally releases the managed resources.</summary>
        /// <param name="isManagedCall">true to release both managed and unmanaged resources; false to release only unmanaged resources. </param>
        protected virtual ValueTask DisposeAsync(bool isManagedCall)
        {
            Dispose(isManagedCall);
            return ValueTask.CompletedTask;
        }

        #endregion
    }
}
