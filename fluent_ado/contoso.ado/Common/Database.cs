//===============================================================================
// Concepts almost completely re-written, but originally taken from:
// Microsoft patterns & practices Enterprise Library Data Access Application Block
// Copyright © Microsoft Corporation.  All rights reserved.
// The concept of 'Data Action' for processing results of command execution is
// based on a StackOverflow discussion at https://stackoverflow.com/a/24992968/458354
// for patterns around async processing of data streams.
//===============================================================================
using System.Data;
using System.Data.Common;
using contoso.ado.EventListeners;
using contoso.ado.Fluent;
using contoso.ado.Internals;

namespace contoso.ado.Common
{
    /// <summary>Represents an abstract database that ADO commands can be run against. </summary>
    /// <remarks>
    /// The <see cref="Database"/> class leverages the provider factory model from ADO.NET. A database instance holds 
    /// a reference to a concrete <see cref="DbProviderFactory"/> object to which it forwards the creation of ADO.NET objects.
    /// </remarks>
    public abstract class Database : IEquatable<Database>
    {
        #region Ctor and Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="Database"/> class with a connection string 
        /// and a <see cref="DbProviderFactory"/>.
        /// </summary>
        /// <param name="connectionString">The connection string to the database.</param>
        /// <param name="dbProviderFactory">A <see cref="DbProviderFactory"/> object.</param>
        protected Database(string connectionString, DbProviderFactory dbProviderFactory)
        {
            ParamCheck.Assert.IsNotNullOrEmpty(connectionString, "connectionString");
            ParamCheck.Assert.IsNotNull(dbProviderFactory, "dbProviderFactory");

            this.ConnectionString = connectionString;
            this.DbProviderFactory = dbProviderFactory;
        }

        /// <summary>Provides the underlying <see cref="IDataModuleObserver"/> instance to derived classes.</summary>
        protected static ContextLog DataModuleObserver { get { return ContextLog.Q; } }

        #endregion

        #region Provider Specific

        /// <summary>Gets the <see cref="DbProviderFactory"/> used by the database instance.</summary>
        internal DbProviderFactory DbProviderFactory { get; }

        #endregion

        #region Connection Management

        /// <summary>Gets the string used to open a database.</summary>
        /// <value>The string used to open a database.</value>
        /// <seealso cref="DbConnection.ConnectionString"/>
        protected internal string ConnectionString { get; }

        /// <summary>Creates a new, unopened <see cref="DbConnection"/> instance to this database.</summary>
        /// <returns>The <see cref="DbConnection"/> for this database.</returns>
        /// <seealso cref="DbConnection"/>        
        protected internal virtual DbConnection CreateConnection()
        {
            DbConnection? newConnection = null;
            try
            {
                newConnection = DbProviderFactory.CreateConnection();
                newConnection!.ConnectionString = ConnectionString;
                newConnection.StateChange += ContextLog.Q.ConnectionStateChanged;
                return newConnection;
            }
            catch (Exception createConnEx)
            {
                ContextLog.Q.ConnectionFailed(newConnection?.ToVerboseDebugString(), createConnEx);
                newConnection?.SafeClose();
                throw;
            }
        }

        /// <summary>Creates a new, opened <see cref="DbConnection"/> instance to this database.</summary>
        /// <returns>The <see cref="DbConnection"/> for this database.</returns>
        /// <seealso cref="DbConnection"/>        
        protected internal virtual DbConnection CreateOpenedConnection()
        {
            DbConnection newConnection = CreateConnection();
            try
            {
                newConnection.Open();
                return newConnection;
            }
            catch (Exception openConnEx)
            {
                ContextLog.Q.ConnectionFailed(newConnection?.ToVerboseDebugString(), openConnEx);
                newConnection?.SafeClose();
                throw;
            }
        }

        /// <summary>Creates a new, opened <see cref="DbConnection"/> instance to this database.</summary>
        /// <returns>The <see cref="DbConnection"/> for this database.</returns>
        /// <seealso cref="DbConnection"/>        
        protected internal virtual async Task<DbConnection> CreateOpenedConnectionAsync()
        {
            DbConnection newConnection = CreateConnection();
            try
            {
                await newConnection.OpenAsync().ConfigureAwait(false);
                return newConnection;
            }
            catch (Exception openConnEx)
            {
                ContextLog.Q.ConnectionFailed(newConnection.ToVerboseDebugString(), openConnEx);
                newConnection.SafeClose();
                throw;
            }
        }

        #endregion

        #region Parameter Management

        /// <summary>Returns a new instance of a <see cref="DbParameter"/>.</summary>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <returns>An unconfigured parameter.</returns>
        public DbParameter CreateParameter(string parameterName)
        {
            ParamCheck.Assert.IsNotNullOrEmpty(parameterName, "parameterName");
            DbParameter param = DbProviderFactory.CreateParameter()!;
            param.ParameterName = FormatParameterName(parameterName);
            return param;
        }

        /// <summary>Returns a new instance of a <see cref="DbParameter"/> for holding xml content.</summary>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <returns>An parameter configured as a provider specific xml parameter type.</returns>
        public virtual DbParameter CreateXmlParameter(string parameterName)
        {
            DbParameter param = CreateParameter(parameterName);
            param.DbType = DbType.Xml;
            return param;
        }

        /// <summary>Returns a new instance of a <see cref="DbParameter"/> configured with Direction 'Output'.</summary>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <returns>An unconfigured output parameter.</returns>
        public DbParameter CreateOutputParameter(string parameterName)
        {
            DbParameter param = CreateParameter(parameterName);
            param.Direction = ParameterDirection.Output;
            return param;
        }

        /// <summary>Builds a value parameter name for the current database.</summary>
        /// <remarks>
        /// A derived Database class can override this to format a plain text parameter name
        /// with any special characters. For example, A SQL Server implementation can override
        /// this to ensure each parameter name is prefixed with '@'. Thus, the use of parameter names
        /// can be done database agnostically, where end-users only send in the text portion of the parameter name.
        /// </remarks>
        /// <param name="name">The name of the parameter.</param>
        /// <returns>A correctly formated parameter name.</returns>
        protected virtual string FormatParameterName(string name) { return name; }

        #endregion

        #region Command Management

        /// <summary>Gets or sets the wait time before terminating the attempt to execute a command and generating an error. Default is 180 seconds.</summary>
        public virtual int CommandTimeout { get; set; } = 180;

        /// <summary>Creates a <see cref="DbCommand"/> for a SQL query string.</summary>
        /// <param name="queryString">The text of the query.</param>        
        /// <returns>The <see cref="DbCommand"/> for the SQL query.</returns>        
        protected internal virtual DbCommand CreateCommandFromSqlString(string queryString)
        {
            ParamCheck.Assert.IsNotNullOrEmpty(queryString, "queryString");
            return CreateCommandByCommandType(CommandType.Text, queryString);
        }

        /// <summary>Creates a <see cref="DbCommand"/> for a stored procedure.</summary>
        /// <param name="storedProcedureName">The name of the stored procedure.</param>
        /// <returns>The <see cref="DbCommand"/> for the stored procedure.</returns>       
        protected internal virtual DbCommand CreateCommandFromSprocName(string storedProcedureName)
        {
            ParamCheck.Assert.IsNotNullOrEmpty(storedProcedureName, "storedProcedureName");
            return CreateCommandByCommandType(CommandType.StoredProcedure, storedProcedureName);
        }

        DbCommand CreateCommandByCommandType(CommandType commandType, string commandText)
        {
            DbCommand command = DbProviderFactory.CreateCommand()!;
            command.CommandType = commandType;
            command.CommandText = commandText;
            command.CommandTimeout = this.CommandTimeout;
            return command;
        }

        #endregion

        #region Command Contexts

        /// <summary>
        /// Returns the fluent context for the execution of a single <see cref="DbCommand"/>. 
        /// The context is constrained to its connection lifetime.
        /// The connection is opened and closed per command without any external transaction management.
        /// </summary>
        public AdoContext CreateCommand()
        {
            return new AdoContext(this);
        }

        /// <summary>
        /// Returns the fluent context for the execution of one or more <see cref="DbCommand"/>s under a single transaction.
        /// The context uses a single connection and local <see cref="DbTransaction"/>.
        /// Multiple commands to the same database may be executed within this context.
        /// When the context goes out of scope, the transaction is committed (or rolled back if an error occurred) and the connection is closed.
        /// The supplied <paramref name="transactionContext"/> should have been created prior to enlistment and 
        /// managed within the context of a <see langword="using"/> statement.
        /// </summary>
        public AdoTransactionContext CreateCommandEnlistedIn(TransactionContext transactionContext)
        {
            return new AdoTransactionContext(this, transactionContext);
        }

        /// <summary>
        /// Returns the context for the execution of a single <see cref="DbCommand"/>. 
        /// The context is constrained to its connection lifetime.
        /// The connection is opened and closed per command without any external transaction management.
        /// </summary>
        protected internal virtual CommandContext CreateCommandContext(DbCommand cmd)
        {
            return new CommandContext(cmd, this);
        }

        /// <summary>
        /// Returns the context for the execution of one or more <see cref="DbCommand"/>s under a single transaction.
        /// The context uses a single connection and local <see cref="DbTransaction"/>.
        /// Multiple commands to the same database may be executed within this context.
        /// When the context goes out of scope, the transaction is committed (or rolled back if an error occurred) and the connection is closed.
        /// The supplied <paramref name="transactionContext"/> should have been created prior to enlistment and 
        /// managed within the context of a <see langword="using"/> statement.
        /// </summary>
        protected internal virtual EnlistedCommandContext CreateTransactionCommandContext(DbCommand cmd, TransactionContext transactionContext)
        {
            return new EnlistedCommandContext(cmd, this, transactionContext);
        }

        /// <summary>
        /// Returns the context for the iterative execution of a <see cref="DbCommand"/> under a single transaction
        /// for each item in a collection of <typeparamref name="T"/>.
        /// The context uses a single connection and local <see cref="DbTransaction"/>.
        /// Each iteration of the command is executed within this context.
        /// When the context goes out of scope, the transaction is committed (or rolled back if an error occurred) and the connection is closed.
        /// The supplied <paramref name="transactionContext"/> should have been created prior to enlistment and 
        /// managed within the context of a <see langword="using"/> statement.
        /// </summary>
        protected internal virtual EnlistedBulkOperationContext<T> CreateBulkOperationCommandContext<T>(DbCommand cmd, TransactionContext transactionContext)
        {
            return new EnlistedBulkOperationContext<T>(cmd, this, transactionContext);
        }

        #endregion

        #region Transaction Managment

        /// <summary>
        /// Initiates the scope for an open connection and <see cref="DbTransaction"/>.
        /// This call should be wrapped in a <see langword="using"/> statement.
        /// </summary>
        public TransactionContext BeginLocalTransaction()
        {
            return TransactionContext.Create(this);
        }

        /// <summary>
        /// Initiates the scope for an open connection and <see cref="DbTransaction"/>.
        /// This call should be wrapped in a <see langword="using"/> statement.
        /// </summary>
        public async Task<TransactionContext> BeginLocalTransactionAsync()
        {
            return await TransactionContext.CreateAsync(this).ConfigureAwait(false);
        }

        #endregion

        #region IEquatable

        int _hash;

        /// <summary>Returns the hash of the ConnectionString.</summary>
        public override int GetHashCode()
        {
            if (_hash == 0 && !String.IsNullOrWhiteSpace(this.ConnectionString))
                _hash = this.ConnectionString.GetHashCode();
            return _hash;
        }

        /// <summary>Compares equality by ConnectionString.</summary>
        public override bool Equals(object? obj)
        {
            return obj is Database other && Equals(other);
        }

        /// <summary>Compares equality by ConnectionString.</summary>
        public bool Equals(Database? other)
        {
            if (other == null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return this.GetHashCode() == other.GetHashCode() && String.Equals(this.ConnectionString, other.ConnectionString);
        }

        #endregion

    }
}
