using System.Data.Common;
using contoso.ado.Common;
using contoso.ado.Internals;
//using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;

namespace contoso.ado.PostgreSql
{
    /// <summary>Represents a PostgreSQL database.</summary>
    /// <remarks>Internally, uses Npgsql Managed Provider from the Npgsql development team to connect to the database.</remarks>
    public class PostgresDatabase : Database
    {
        const string _parameterToken = "@";
        static bool _withInjectedPrepare = true;
        //readonly static JsonSerializerSettings _jsonDbSetting = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };

        #region Ctor and Startup

        /// <summary>Initializes a new instance of the <see cref="PostgresDatabase"/> class with a connection string.</summary>
        /// <param name="connectionString">The connection string.</param>
        public PostgresDatabase(string connectionString)
            : base(connectionString, NpgsqlFactory.Instance)
        {
            //// set Json.Net as our default serializer for anything json or jsonb
            //// https://www.npgsql.org/doc/types/jsonnet.html
            //// this invokes the npgsql api directly
            //NpgsqlConnection.GlobalTypeMapper.UseJsonNet(settings: _jsonDbSetting);
            //// not setup a pass-thru to the GlobalTypeMapper so that consuming code doesn't need an explicit npgsql library reference
            //GlobalTypeMapper = new GlobalTypeMapper(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgresDatabase"/> class with the given connection string parameters.
        /// Assumes user name is same as <paramref name="database"/>.
        /// </summary>
        public static PostgresDatabase CreateFor(string host, string database, string pwd)
        {
            return CreateFor(host: host, database: database, user: database, pwd: pwd);
        }

        /// <summary>Initializes a new instance of the <see cref="PostgresDatabase"/> class with the given connection string parameters.</summary>
        public static PostgresDatabase CreateFor(string host, string database, string user, string pwd, ushort port = 5432, NpgsqlConnectionParams? connectionParams = null)
        {
            //string withTableComposites = loadTableComposites ? "Load Table Composites=true;" : String.Empty;
            // support has been added to call 'Prepare' inline
            //string connectionString = $"Host={host};Port={port};Database={database};Username={user};Password={pwd};Max Auto Prepare=64;Auto Prepare Min Usages=2;";

            if (connectionParams == null)
            {
                string connectionString = $"Host={host};Port={port};Database={database};Username={user};Password={pwd};";
                return new PostgresDatabase(connectionString);
            }
            else
            {
                NpgsqlConnectionParams p = connectionParams;
                string connectionString = $"Host={host};Port={port};Database={database};Username={user};Password={pwd};Timeout={p.Timeout};Command Timeout={p.CommandTimeout};Pooling={(p.Pooling ? "true" : "false")};Minimum Pool Size={p.MinimumPoolSize};Maximum Pool Size={p.MaximumPoolSize};Connection Idle Lifetime={p.ConnectionIdleLifetime};Read Buffer Size={p.ReadBufferSize};Write Buffer Size={p.WriteBufferSize};Application Name='{p.ApplicationName}';Include Error Detail={p.IncludeErrorDetail};";
                _withInjectedPrepare = p.WithInjectedPrepare;
                return new PostgresDatabase(connectionString) { CommandTimeout = p.CommandTimeout };
            }
        }

        ///// <summary>A pass-thru to the <see cref="NpgsqlConnection.GlobalTypeMapper"/> so that consuming code doesn't need an explicit npgsql library reference.</summary>
        //public GlobalTypeMapper GlobalTypeMapper { get; }

        /// <summary>Creates a new, opened <see cref="DbConnection"/> instance to this database.</summary>
        /// <returns>The <see cref="DbConnection"/> for this database.</returns>
        /// <seealso cref="DbConnection"/>        
        internal DbConnection CreateAndOpenConnection()
        {
            return base.CreateOpenedConnection();
        }

        /// <summary>Creates a new, opened <see cref="DbConnection"/> instance to this database.</summary>
        /// <returns>The <see cref="DbConnection"/> for this database.</returns>
        /// <seealso cref="DbConnection"/>        
        internal Task<DbConnection> CreateAndOpenConnectionAsync()
        {
            return base.CreateOpenedConnectionAsync();
        }

        #endregion

        #region Parameter Management

        /// <summary>Returns a new instance of a <see cref="NpgsqlParameter"/> for holding xml content.</summary>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <returns>An parameter configured as a provider specific xml parameter type.</returns>
        public override DbParameter CreateXmlParameter(string parameterName)
        {
            NpgsqlParameter param = base.CreateParameter(parameterName).AsNpgsqlParameter();
            param.NpgsqlDbType = NpgsqlDbType.Xml;
            return param;
        }

        /// <summary>
        /// Checks if the given <paramref name="parameterName"/> starts with the Npgsql Server parameter token '@' and inserts it at the start if it does not.  
        /// </summary>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <returns>A correctly formated parameter name.</returns>
        protected override string FormatParameterName(string parameterName)
        {
            ParamCheck.Assert.IsNotNullOrEmpty(parameterName, "parameterName");
            return parameterName.StartsWith(_parameterToken) ? parameterName : parameterName.Insert(0, _parameterToken);
        }

        #endregion

        #region Command Contexts

        /// <summary>
        /// Returns the Npgsql specific context for the execution of a single <see cref="DbCommand"/>. 
        /// The context is constrained to its connection lifetime.
        /// The connection is opened and closed per command without any external transaction management.
        /// </summary>
        protected override CommandContext CreateCommandContext(DbCommand cmd)
        {
            return new PostgresCommandContext(cmd.AsNpgsqlCommand(), this, _withInjectedPrepare);
        }

        #endregion
    }
}
