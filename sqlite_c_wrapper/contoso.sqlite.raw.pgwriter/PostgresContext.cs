using System.Data.Common;
using contoso.ado.PostgreSql;
using Npgsql;

namespace contoso.sqlite.raw.pgwriter
{
    /// <summary>Configure a connection to PostgreSql for the purposes of a bulk binary COPY operation from Sqlite.</summary>
    public class PostgresContext
    {
        readonly string _connectionString;

        PostgresContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>Configure the connection to Postgres using default port and user name same as database name.</summary>
        public static PostgresContext CreateFor(string host, string database, string pwd) => CreateFor(host, database, user: database, pwd, port: 5432);

        /// <summary>Configure the connection to Postgres.</summary>
        public static PostgresContext CreateFor(string host, string database, string user, string pwd, ushort port = 5432, NpgsqlConnectionParams? connectionParams = null)
        {
            //string withTableComposites = loadTableComposites ? "Load Table Composites=true;" : String.Empty;
            // support has been added to call 'Prepare' inline
            //string connectionString = $"Host={host};Port={port};Database={database};Username={user};Password={pwd};Max Auto Prepare=64;Auto Prepare Min Usages=2;";

            if (connectionParams == null)
            {
                string connectionString = $"Host={host};Port={port};Database={database};Username={user};Password={pwd};";
                return new PostgresContext(connectionString);
            }
            else
            {
                NpgsqlConnectionParams p = connectionParams;
                string connectionString = $"Host={host};Port={port};Database={database};Username={user};Password={pwd};Timeout={p.Timeout};Command Timeout={p.CommandTimeout};Pooling={(p.Pooling ? "true" : "false")};Minimum Pool Size={p.MinimumPoolSize};Maximum Pool Size={p.MaximumPoolSize};Connection Idle Lifetime={p.ConnectionIdleLifetime};Read Buffer Size={p.ReadBufferSize};Write Buffer Size={p.WriteBufferSize};Application Name='{p.ApplicationName}';Include Error Detail={p.IncludeErrorDetail};";
                return new PostgresContext(connectionString);
            }
        }

        /// <summary>Returns a context for configuring the COPY statement.</summary>
        public WriterConfiguration WriterConfiguration => new WriterConfiguration(_connectionString);

        /// <summary>Convenience method to execute <paramref name="sql"/> before or after the COPY operation.</summary>
        /// <param name="sql">A postgres sql statement to execute.</param>
        /// <param name="cmdDelegate">An optional delegate which will be invoked before executing the command, such as for adding a parameter value.</param>
        public async Task<int> ExecuteNonQueryAsync(string sql, Action<DbCommand>? cmdDelegate = null)
        {
            NpgsqlConnection conn;
            await using ((conn = new NpgsqlConnection(_connectionString)).ConfigureAwait(false))
            {
                await conn.OpenAsync().ConfigureAwait(false);
                using var cmd = new NpgsqlCommand(sql, conn);
                cmdDelegate?.Invoke(cmd);
                await cmd.PrepareAsync().ConfigureAwait(false);
                return await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        /// <summary>Convenience method to execute <paramref name="sql"/> before or after the COPY operation and obtain a scalar value.</summary>
        /// <param name="sql">A postgres sql statement to execute.</param>
        /// <param name="cmdDelegate">An optional delegate which will be invoked before executing the command, such as for adding a parameter value.</param>
        public async Task<object?> ExecuteScalarAsync(string sql, Action<DbCommand>? cmdDelegate = null)
        {
            NpgsqlConnection conn;
            await using ((conn = new NpgsqlConnection(_connectionString)).ConfigureAwait(false))
            {
                await conn.OpenAsync().ConfigureAwait(false);
                using var cmd = new NpgsqlCommand(sql, conn);
                cmdDelegate?.Invoke(cmd);
                await cmd.PrepareAsync().ConfigureAwait(false);
                return await cmd.ExecuteScalarAsync().ConfigureAwait(false);
            }
        }
    }
}
