using System;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;
using contoso.ado.Common;
using contoso.ado.Internals;
//using Newtonsoft.Json;
using Npgsql;

namespace contoso.ado.PostgreSql
{
    /// <summary>
    /// Postgres/Npgsql specific implementation for the context wrapping a stronly typed <see cref="NpgsqlConnection"/> or <see cref="NpgsqlCommand"/>
    /// to support provider specific features.
    /// The context is constrained to its connection lifetime.
    /// The connection is opened and closed per command without any external transaction management.
    /// </summary>
    public sealed class PostgresCommandContext : CommandContext
    {
        //readonly static JsonSerializerSettings _jsonDbSetting = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
        readonly PostgresDatabase _npgsqlContext;

        internal PostgresCommandContext(NpgsqlCommand cmd, PostgresDatabase dbContext, bool withInjectedPrepare = false)
            : base(cmd, dbContext)
        {
            _npgsqlContext = dbContext;
            Command = cmd;
            if(withInjectedPrepare)
                // 2023-03 adding this with hope 'Prepare() will now be called automatically with each command
                base.PreExecute((cmd) => cmd.AsNpgsqlCommand().Prepare());
        }

        internal NpgsqlCommand Command { get; }

        internal ContextLog ContextLog => DataModuleObserver;

        internal NpgsqlConnection CreateNpgsqlConnection()
        {
            DbConnection? connection = null;
            try
            {
                return (connection = _npgsqlContext.CreateAndOpenConnection()).AsNpgsqlConnection();
            }
            catch
            {
                connection?.SafeClose();
                throw;
            }
        }

        #region Support for query plan 'Prepare'

        /// <summary>
        /// Creates a server-side prepared statement on the PostgreSQL server. This will make repeated future executions of this command much faster.
        /// Note that all parameters must be set before calling Prepare().You must also set the DbType or NpgsqlDbType on your parameters to unambiguously specify the data type.
        /// </summary>
        public PostgresCommandContext Prepare()
        {
            base.PreExecute((cmd) => cmd.AsNpgsqlCommand().Prepare());
            return this;
        }

        /// <summary>
        /// Unprepares a command, closing server-side statements associated with it.
        /// Note that this only affects commands explicitly prepared with Prepare(), not automatically prepared statements.
        /// </summary>
        public PostgresCommandContext Unprepare()
        {
            base.PreExecute((cmd) => cmd.AsNpgsqlCommand().Unprepare());
            return this;
        }

        /// <summary>Unprepares all prepared statements on this connection.</summary>
        public PostgresCommandContext UnprepareAll()
        {
            base.PreExecute((cmd) => cmd.Connection!.AsNpgsqlConnection().UnprepareAll());
            return this;
        }

        #endregion

        #region Support for connection specific type mapping

        ///// <summary>
        ///// Call if you're creating non-predefined PostgreSQL types within your program, to make sure Npgsql becomes properly aware of them.
        ///// Flushes the type cache for this connection's connection string and reloads the types for this connection only.
        ///// Type changes will appear for other connections only after they are re-opened from the pool.
        ///// </summary>
        //public PostgresCommandContext ReloadTypes()
        //{
        //    base.PreExecute((cmd) => cmd.Connection.AsNpgsqlConnection().ReloadTypes());
        //    return this;
        //}

        ///// <summary>Maps a CLR type to a PostgreSQL composite type for this connection only, until closed.</summary>
        ///// <param name="pgCompositeType">
        ///// The PostgreSQL type name for the composite type. If null, the SnakeCaseNameTranslator
        ///// will translate the CLR type (e.g. 'MyClass') to its snake case equivalent (e.g. 'my_class')
        ///// </param>
        //public PostgresCommandContext MapComposite<T>(string pgCompositeType = null)
        //{
        //    base.PreExecute((cmd) => cmd.Connection.AsNpgsqlConnection().TypeMapper.MapComposite<T>(pgCompositeType));
        //    return this;
        //}

        ///// <summary>Maps a CLR enum to a PostgreSQL enum type for this connection only, until closed.</summary>
        ///// <param name="pgEnumType">
        ///// The PostgreSQL type name for the composite type. If null, the SnakeCaseNameTranslator
        ///// will translate the enum type (e.g. 'MyEnum') to its snake case equivalent (e.g. 'my_enum')
        ///// </param>
        //public PostgresCommandContext MapEnum<TEnum>(string pgEnumType = null)
        //    where TEnum : struct, Enum
        //{
        //    base.PreExecute((cmd) => cmd.Connection.AsNpgsqlConnection().TypeMapper.MapEnum<TEnum>(pgEnumType));
        //    return this;
        //}

        ///// <summary>
        ///// Specify one or more types for which Json.Net mappings to json or jsonb should be precompiled for this connection only, until closed..
        ///// A type can be a CLR type and also an array type, such as <c>typeof(int[])</c>.
        ///// </summary>
        //public PostgresCommandContext UseJsonNet(Type[] clrTypes)
        //{
        //    if (clrTypes._IsNotNullOrEmpty())
        //    {
        //        base.PreExecute((cmd) => cmd.Connection.AsNpgsqlConnection().TypeMapper.UseJsonNet(jsonbClrTypes: clrTypes, jsonClrTypes: clrTypes, settings: _jsonDbSetting));
        //    }
        //    return this;
        //}

        ///// <summary>Sets up NodaTime mappings for the PostgreSQL date/time types for this connection only, until closed..</summary>
        //public PostgresCommandContext UseNodaTime()
        //{
        //    base.PreExecute((cmd) => cmd.Connection.AsNpgsqlConnection().TypeMapper.UseNodaTime());
        //    return this;
        //}

        #endregion

        #region Binary Copy Operations

        /// <summary>
        /// Provide the delegate to read from the <see cref="NpgsqlBinaryExporter"/> returned by <see cref="M:Npgsql.NpgsqlConnection.BeginBinaryExport"/>
        /// on the <see cref="NpgsqlConnection"/> in context
        /// <para>
        /// The underlying command text is of the format: "COPY tbl (col1, col2) TO STDIN BINARY".
        /// </para>
        /// </summary>
        /// <param name="readData">
        /// A delegate that will invoke 'StartRow()' for each row and 'Read()' for each column of the data exported from Postgres.
        /// </param>
        /// <typeparam name="TOut">
        /// Placeholder for the output from reading the bulk exported data. This can be the data itself (in a collection)
        /// or a simple bool success flag if the data is processed and no longer needed, or even a null object;
        /// </typeparam>
        /// <remarks>Binary copy is the most efficient mode for if the data needs to be handled and there is no requirement for plain text.</remarks>
        public BinaryExport<TOut> WithBinaryCopyReader<TOut>(Func<NpgsqlBinaryExporter, TOut> readData) => new BinaryExport<TOut>(this, readData);

        /// <summary>
        /// Provide the async delegate to read from the <see cref="NpgsqlBinaryExporter"/> returned by <see cref="M:Npgsql.NpgsqlConnection.BeginBinaryExport"/>
        /// on the <see cref="NpgsqlConnection"/> in context
        /// <para>
        /// The underlying command text is of the format: "COPY tbl (col1, col2) TO STDIN BINARY".
        /// </para>
        /// </summary>
        /// <param name="readDataAsync">
        /// A delegate that will invoke 'StartRowAsync()' for each row and 'ReadAsync()' for each column of the data exported from Postgres.
        /// </param>
        /// <typeparam name="TOut">
        /// Placeholder for the output from reading the bulk exported data. This can be the data itself (in a collection)
        /// or a simple bool success flag if the data is processed and no longer needed, or even a null object;
        /// </typeparam>
        /// <remarks>Binary copy is the most efficient mode for if the data needs to be handled and there is no requirement for plain text.</remarks>
        public AsyncBinaryExport<TOut> WithAsyncBinaryCopyReader<TOut>(Func<NpgsqlBinaryExporter, Task<TOut>> readDataAsync) => new AsyncBinaryExport<TOut>(this, readDataAsync);

        /// <summary>
        /// Provide the delegate to write to the <see cref="NpgsqlBinaryImporter"/> returned by <see cref="M:Npgsql.NpgsqlConnection.BeginBinaryImport"/>
        /// on the <see cref="NpgsqlConnection"/> in context. 
        /// <para>
        /// The underlying command text is of the format: "COPY tbl (col1, col2) FROM STDIN BINARY".
        /// </para>
        /// This method will close the writer (Complete()) when the delegate finishes its iteration.
        /// </summary>
        /// <param name="writeData">
        /// A delegate that will invoke 'StartRow()' for each row and 'Write()' for each column of the data to be imported into Postgres
        /// </param>
        /// <typeparam name="TIn">
        /// Placeholder for input data if needed. This can be the data to import into Postgres (as a collection)
        /// or if the source is provided by the delegate handler, <typeparamref name="TIn"/> can be any discardable object.
        /// </typeparam>
        /// <remarks>Binary copy is the most efficient mode for if the data needs to be handled and there is no requirement for plain text.</remarks>
        public BinaryImport<TIn> WithBinaryCopyWriter<TIn>(Action<TIn, NpgsqlBinaryImporter> writeData) => new BinaryImport<TIn>(this, writeData);

        /// <summary>
        /// Provide the async delegate to write to the <see cref="NpgsqlBinaryImporter"/> returned by <see cref="M:Npgsql.NpgsqlConnection.BeginBinaryImport"/>
        /// on the <see cref="NpgsqlConnection"/> in context. 
        /// <para>
        /// The underlying command text is of the format: "COPY tbl (col1, col2) FROM STDIN BINARY".
        /// </para>
        /// This method will close the writer (Complete()) when the delegate finishes its iteration.
        /// </summary>
        /// <param name="writeDataAsync">
        /// A delegate that will invoke 'StartRow()' for each row and 'Write()' for each column of the data to be imported into Postgres
        /// </param>
        /// <typeparam name="TIn">
        /// Placeholder for input data if needed. This can be the data to import into Postgres (as a collection)
        /// or if the source is provided by the delegate handler, <typeparamref name="TIn"/> can be any discardable object.
        /// </typeparam>
        /// <remarks>Binary copy is the most efficient mode for if the data needs to be handled and there is no requirement for plain text.</remarks>
        public AsyncBinaryImport<TIn> WithAsyncBinaryCopyWriter<TIn>(Func<TIn, NpgsqlBinaryImporter, Task> writeDataAsync) => new AsyncBinaryImport<TIn>(this, writeDataAsync);

        #endregion

        #region Text Copy Operations

        /// <summary>
        /// Provide the delegate to read from the <see cref="TextReader"/> returned by <see cref="M:Npgsql.NpgsqlConnection.BeginTextExport"/> 
        /// on the <see cref="NpgsqlConnection"/> in context
        /// <para>
        /// The underlying command text is of the format: "COPY tbl (col1, col2) TO STDIN".
        /// </para>
        /// </summary>
        /// <param name="readData">
        /// A delegate that will use the provided <see cref="TextReader"/> to read all data exported from Postgres.
        /// </param>
        /// <typeparam name="TOut">
        /// Placeholder for the output from reading the bulk exported data. This can be the data itself (in a collection)
        /// or a simple bool success flag if the data is processed and no longer needed, or even a null object;
        /// </typeparam>
        /// <remarks>Text copy is the least efficient mode but accomodates the requirement for plain text as source or destination.</remarks>
        public TextExport<TOut> WithTextCopyReader<TOut>(Func<TextReader, TOut> readData) => new TextExport<TOut>(this, readData);

        /// <summary>
        /// Provide the async delegate to read from the <see cref="TextReader"/> returned by <see cref="M:Npgsql.NpgsqlConnection.BeginTextExport"/> 
        /// on the <see cref="NpgsqlConnection"/> in context
        /// <para>
        /// The underlying command text is of the format: "COPY tbl (col1, col2) TO STDIN".
        /// </para>
        /// </summary>
        /// <param name="readDataAsync">
        /// A delegate that will use the provided <see cref="TextReader"/> to read all data exported from Postgres.
        /// </param>
        /// <typeparam name="TOut">
        /// Placeholder for the output from reading the bulk exported data. This can be the data itself (in a collection)
        /// or a simple bool success flag if the data is processed and no longer needed, or even a null object;
        /// </typeparam>
        /// <remarks>Text copy is the least efficient mode but accomodates the requirement for plain text as source or destination.</remarks>
        public AsyncTextExport<TOut> WithAsyncTextCopyReader<TOut>(Func<TextReader, Task<TOut>> readDataAsync) => new AsyncTextExport<TOut>(this, readDataAsync);

        /// <summary>
        /// Provide the delegate to write with the <see cref="TextWriter"/> returned by <see cref="M:Npgsql.NpgsqlConnection.BeginTextImport"/>
        /// on the <see cref="NpgsqlConnection"/> in context
        /// <para>
        /// The underlying command text is of the format: "COPY tbl (col1, col2) FROM STDIN".
        /// </para>
        /// </summary>
        /// <param name="writeData">
        /// A delegate that will use the provided <see cref="TextWriter"/> to write all source data to Postgres.
        /// Each row is written as a single, tab-delimited string. Caller should use the 'Write()' method with the content ending in LF (char(10))
        /// and not use WriteLine();
        /// </param>
        /// <typeparam name="TIn">
        /// Placeholder for input data if needed. This can be the data to import into Postgres (as a collection)
        /// or if the source is provided by the delegate handler, <typeparamref name="TIn"/> can be any discardable object.
        /// </typeparam>
        /// <remarks>Text copy is the least efficient mode but accomodates the requirement for plain text as source or destination.</remarks>
        public TextImport<TIn> WithTextCopyWriter<TIn>(Action<TIn, TextWriter> writeData) => new TextImport<TIn>(this, writeData);

        /// <summary>
        /// Provide the async delegate to write with the <see cref="TextWriter"/> returned by <see cref="M:Npgsql.NpgsqlConnection.BeginTextImport"/>
        /// on the <see cref="NpgsqlConnection"/> in context
        /// <para>
        /// The underlying command text is of the format: "COPY tbl (col1, col2) FROM STDIN".
        /// </para>
        /// </summary>
        /// <param name="writeDataAsync">
        /// A delegate that will use the provided <see cref="TextWriter"/> to write all source data to Postgres.
        /// Each row is written as a single, tab-delimited string. Caller should use the 'Write()' method with the content ending in LF (char(10))
        /// and not use WriteLine();
        /// </param>
        /// <typeparam name="TIn">
        /// Placeholder for input data if needed. This can be the data to import into Postgres (as a collection)
        /// or if the source is provided by the delegate handler, <typeparamref name="TIn"/> can be any discardable object.
        /// </typeparam>
        /// <remarks>Text copy is the least efficient mode but accomodates the requirement for plain text as source or destination.</remarks>
        public AsyncTextImport<TIn> WithAsyncTextCopyWriter<TIn>(Func<TIn, TextWriter, Task> writeDataAsync) => new AsyncTextImport<TIn>(this, writeDataAsync);

        #endregion

        #region Raw Binary Copy Operations

        /// <summary>
        /// Provide the delegate to read the <see cref="NpgsqlRawCopyStream"/> returned by <see cref="M:Npgsql.NpgsqlConnection.BeginRawBinaryCopy"/>
        /// on the <see cref="NpgsqlConnection"/> in context
        /// <para>
        /// The underlying command text is of the format: "COPY tbl (col1, col2) TO STDIN BINARY".
        /// </para>
        /// </summary>
        /// <param name="readData">A delegate that will read the <see cref="Stream"/> exported from Postgres.</param>
        /// <typeparam name="TOut">
        /// Placeholder for the output from reading the bulk exported data. This can be the data itself (as a byte array or memorystream)
        /// or a simple bool success flag if the data is processed and no longer needed, or even a null object;
        /// </typeparam>
        /// <remarks>
        /// Raw Binary copy is the most efficient mode for copying and restoring a table as is.
        /// The data is written as a blob and can only be interpreted by the Postgres engine when restored.
        /// </remarks>
        public RawBinaryExport<TOut> WithBlobCopyReader<TOut>(Func<Stream, TOut> readData) => new RawBinaryExport<TOut>(this, readData);

        /// <summary>
        /// Provide the async delegate to read the <see cref="NpgsqlRawCopyStream"/> returned by <see cref="M:Npgsql.NpgsqlConnection.BeginRawBinaryCopy"/>
        /// on the <see cref="NpgsqlConnection"/> in context
        /// <para>
        /// The underlying command text is of the format: "COPY tbl (col1, col2) TO STDIN BINARY".
        /// </para>
        /// </summary>
        /// <param name="readDataAsync">A delegate that will read the <see cref="Stream"/> exported from Postgres.</param>
        /// <typeparam name="TOut">
        /// Placeholder for the output from reading the bulk exported data. This can be the data itself (as a byte array or memorystream)
        /// or a simple bool success flag if the data is processed and no longer needed, or even a null object;
        /// </typeparam>
        /// <remarks>
        /// Raw Binary copy is the most efficient mode for copying and restoring a table as is.
        /// The data is written as a blob and can only be interpreted by the Postgres engine when restored.
        /// </remarks>
        public AsyncRawBinaryExport<TOut> WithAsyncBlobCopyReader<TOut>(Func<Stream, Task<TOut>> readDataAsync) => new AsyncRawBinaryExport<TOut>(this, readDataAsync);

        /// <summary>
        /// Provide the delegate to write to the <see cref="NpgsqlRawCopyStream"/> returned by <see cref="M:Npgsql.NpgsqlConnection.BeginRawBinaryCopy"/>
        /// on the <see cref="NpgsqlConnection"/> in context
        /// <para>
        /// The underlying command text is of the format: "COPY tbl (col1, col2) FROM STDIN BINARY".
        /// </para>
        /// </summary>
        /// <param name="writeData">A delegate that will copy a Postgres table blob back into Postgres.</param>
        /// <typeparam name="TIn">
        /// Placeholder for input data if needed. This can be the data to import into Postgres (as byte array or stream)
        /// or if the source is provided by the delegate handler, <typeparamref name="TIn"/> can be any discardable object.
        /// </typeparam>
        /// <remarks>
        /// Raw Binary copy is the most efficient mode for copying and restoring a table as is.
        /// The data is written as a blob and can only be interpreted by the Postgres engine when restored.
        /// </remarks>
        public RawBinaryImport<TIn> WithBlobCopyWriter<TIn>(Action<TIn, Stream> writeData) => new RawBinaryImport<TIn>(this, writeData);

        /// <summary>
        /// Provide the async delegate to write to the <see cref="NpgsqlRawCopyStream"/> returned by <see cref="M:Npgsql.NpgsqlConnection.BeginRawBinaryCopy"/>
        /// on the <see cref="NpgsqlConnection"/> in context
        /// <para>
        /// The underlying command text is of the format: "COPY tbl (col1, col2) FROM STDIN BINARY".
        /// </para>
        /// </summary>
        /// <param name="writeDataAsync">A delegate that will copy a Postgres table blob back into Postgres.</param>
        /// <typeparam name="TIn">
        /// Placeholder for input data if needed. This can be the data to import into Postgres (as byte array or stream)
        /// or if the source is provided by the delegate handler, <typeparamref name="TIn"/> can be any discardable object.
        /// </typeparam>
        /// <remarks>
        /// Raw Binary copy is the most efficient mode for copying and restoring a table as is.
        /// The data is written as a blob and can only be interpreted by the Postgres engine when restored.
        /// </remarks>
        public AsyncRawBinaryImport<TIn> WithAsyncBlobCopyWriter<TIn>(Func<TIn, Stream, Task> writeDataAsync) => new AsyncRawBinaryImport<TIn>(this, writeDataAsync);

        #endregion

    }
}
