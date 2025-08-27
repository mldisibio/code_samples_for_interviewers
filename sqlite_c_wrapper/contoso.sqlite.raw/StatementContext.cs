using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace contoso.sqlite.raw
{
    /// <summary>Wraps a sqlite3 prepared statement and its parameter collection, if any.</summary>
    public abstract class StatementContext : OperationContext
    {
        readonly Dictionary<string, int> _namedParams;

        /// <summary>Initialize with a prepared sql statement, its text, and the connection that prepared it.</summary>
        protected StatementContext(SQLitePCL.sqlite3_stmt stmt, SqlHash sqlHash, ConnectionContext connectionCtx)
            : base(stmt, sqlHash, connectionCtx)
        {
            ConnectionContext = connectionCtx;
            try
            {
                ParameterCount = ExtractNamedParameters(stmt, out _namedParams);
            }
            catch (Exception ex)
            {
                throw new SqliteDbException(result: null, msg: "While extracting parameter names", filePath: FilePath, innerException: ex);
            }
        }

        /// <summary>Get the <see cref="ConnectionContext"/> in which this statement was prepared.</summary>
        internal ConnectionContext ConnectionContext { get; }

        /// <summary>The cached <see cref="RowContext"/>, if set by a reader.</summary>
        internal RowContext? RowContext { get; set; }

        /// <summary>Count of bindable parameters, named or indexed only.</summary>
        public int ParameterCount { get; }

        /// <summary>True if <see cref="OperationContext.Statement"/> has any bindable parameters.</summary>
        public bool HasParameters => ParameterCount > 0;

        /// <summary>True if the bindable parameters are named.</summary>
        public bool HasNamedParameters => _namedParams.Count > 0;

        /// <summary>True if the parameters are nameless and bindable by index only.</summary>
        public bool HasNamelessParameters => ParameterCount > 0 && _namedParams.Count == 0;

        /// <summary>Retrieve the index of <paramref name="paramName"/> in the prepared sql statement in context.</summary>
        public int this[string paramName]
        {
            get
            {
                if (HasParameters)
                {
                    if (_namedParams.TryGetValue(paramName, out int paramIdx))
                        return paramIdx;
                    else
                        throw new IndexOutOfRangeException($"Parameter [{paramName}] not found");
                }
                else
                    throw new IndexOutOfRangeException($"Statement has no parameters");
            }
        }

        /// <summary>Caller supplied delgate to set the parameters of the underlying prepared statement.</summary>
        protected Action<BindContext> MapSqlParameters { get; set; } = (_) => { };

        /// <summary>Supply a delegate in which all parameters are mapped once before a single execution of the prepared sql.</summary>
        public StatementContext MapParameters(Action<BindContext> map)
        {
            MapSqlParameters = map;
            return this;
        }

        /// <summary>Supply a delegate in which each row returned from execution of the prepared sql is mapped to an instance of <typeparamref name="T"/>.</summary>
        public QueryContext<T> MapRow<T>(Func<ReadContext, T> mapRow) => new QueryContext<T>(this, mapRow);

        /// <summary>Supply a delegate in which each row returned from execution of the prepared sql is processed by <paramref name="processRow"/>.</summary>
        public ProcessContext ProcessRow(Action<ReadContext> processRow) => new ProcessContext(this, processRow);

        /// <summary>Supply a delegate in which each row returned from execution of the prepared sql is asynchronously processed by <paramref name="processRow"/>.</summary>
        public TaskContext ProcessRowAsync(Func<ReadContext, Task> processRow) => new TaskContext(this, processRow);

        /// <summary>Execute the prepared statement in context and return the first column of the last row returned, as a scalar value.</summary>
        /// <param name="map">Supply a delegate to map the first column of the expected result to the requested scalar <typeparamref name="T"/>.</param>
        public T? ExecuteScalar<T>(Func<ColumnContext, T?> map)
        {
            var ctx = new ScalarCommandContext(this);
            return ctx.ExecuteScalar(map);
        }

        /// <summary>
        /// Creates a dictionary of parameter names and their index, or an empty dictionary if <paramref name="stmt"/> has no parameter placeholders.
        /// Returns the number of bindable parameters (all named or all unnamed).
        /// </summary>
        public static int ExtractNamedParameters(SQLitePCL.sqlite3_stmt stmt, out Dictionary<string, int> namedParameters)
        {
            stmt.ThrowIfInvalid();

            // find the count of parameters;
            // usually this is the count of unique named parameters (anything ":AAA", "$AAA", or "@AAA")
            // or the count of unnamed tokens ("?") which are all considered unique;
            // however its also the largest number when the token format is "?NNN", even if all numbers from 1 to N are not actually used in the statement
            // e.g. if the only parameter in the sql is "?9" the parameter count will be 9 but the only named parameter will be at index 9
            int paramCnt = SQLitePCL.raw.sqlite3_bind_parameter_count(stmt);
            if (paramCnt > 0)
            {
                namedParameters = new Dictionary<string, int>(paramCnt);
                // parameter indices are 1-based;
                for (int pIdx = 1; pIdx <= paramCnt; pIdx++)
                {
                    // find the name for each parameter index; 
                    // the simple "?" token will be unnamed, so binding would be 1-for-1 using position and won't need a lookup;
                    // the "?NNN" format only has a name if the numbered parameter is in the statement;
                    // so bottom line we only need a dictionary if an actual name is found for a given index
                    string pName = SQLitePCL.raw.sqlite3_bind_parameter_name(stmt, pIdx).utf8_to_string();
                    if (pName.IsNotNullOrEmptyString())
                        namedParameters[pName] = pIdx;
                }
                // whether or not the parameters are named, unnamed or are '?NNN' placeholders not actually used, the actual count and indexes are what sqlite tells us
                return paramCnt;
            }
            else
            {
                namedParameters = new Dictionary<string, int>(0);
                return 0;
            }
        }

        /// <summary>Binds any parameter values for the statement in context, after clearing any previous bindings.</summary>
        internal void BindParametersToValuesIfAny()
        {
            if (HasParameters)
            {
                // clear values of any previously bound parameters
                int result = SQLitePCL.raw.sqlite3_clear_bindings(Statement);
                if (result != SQLitePCL.raw.SQLITE_OK)
                    throw FailedExecution(result);

                // bind parameters to runtime values, if any
                if (MapSqlParameters != null)
                {
                    var bindCtx = new BindContext(this);
                    MapSqlParameters(bindCtx);
                }
            }
        }

        /// <summary>Start a sql operation performance timer.</summary>
        internal void StartTicks() => ConnectionContext.StartTicks();

        /// <summary>Ensure the performance timer is stopped, e.g. if operation threw an exception.</summary>
        internal void StopTicks() => ConnectionContext.StopTicks();

        /// <summary>Stop the performance timer and return the milliseconds elapsed.</summary>
        internal long? GetElapsed() => ConnectionContext.GetElapsed();

        /// <summary>Clean up resources referenced by the <see cref="StatementContext"/> itself.</summary>
        protected override void Dispose(bool isManagedCall)
        {
            if (!_isAlreadyDisposed && isManagedCall)
            {
                _namedParams?.Clear();

                // dispose the 'statement' handle
                ConnectionContext.TryFinalizeStatement(Statement);
            }
            base.Dispose(isManagedCall);
        }
    }
}
