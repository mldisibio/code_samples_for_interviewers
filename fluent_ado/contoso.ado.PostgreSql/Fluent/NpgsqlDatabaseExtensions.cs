using System;
using System.Data.Common;
using contoso.ado.Common;
using Npgsql;

namespace contoso.ado.PostgreSql
{
    /// <summary>Helper extensions specific to the <see cref="PostgresDatabase"/>.</summary>
    public static class NpgsqlDatabaseExtensions
    {
        /// <summary>Cast <paramref name="dbCommand"/> to <see cref="NpgsqlCommand"/>. Will throw if cast is invalid.</summary>
        public static NpgsqlCommand AsNpgsqlCommand(this DbCommand dbCommand)
        {
            if (dbCommand is NpgsqlCommand npgsqlCmd)
                return npgsqlCmd;
            else
                throw new InvalidCastException("The given DbCommand is not a NpgsqlCommand.");
        }

        /// <summary>Cast <paramref name="dbConnection"/> to <see cref="NpgsqlConnection"/>. Will throw if cast is invalid.</summary>
        public static NpgsqlConnection AsNpgsqlConnection(this DbConnection dbConnection)
        {
            if (dbConnection is NpgsqlConnection npgsqlConn)
                return npgsqlConn;
            else
                throw new InvalidCastException("The given DbConnection is not a NpgsqlConnection.");
        }

        /// <summary>Cast <paramref name="dataReader"/> to <see cref="NpgsqlDataReader"/>. Will throw if cast is invalid.</summary>
        public static NpgsqlDataReader AsNpgsqlDataReader(this DbDataReader dataReader)
        {
            if (dataReader is NpgsqlDataReader npgsqlReader)
                return npgsqlReader;
            else
                throw new InvalidCastException("The given DbDataReader is not a NpgsqlDataReader.");
        }

        /// <summary>Cast <paramref name="cmdContext"/> to <see cref="PostgresCommandContext"/>. Will throw if cast is invalid.</summary>
        public static PostgresCommandContext AsNpgsqlContext(this CommandContext cmdContext)
        {
            if (cmdContext is PostgresCommandContext npgsqlCmdCtx)
                return npgsqlCmdCtx;
            else
                throw new InvalidCastException("The given CommandContext is not a PostgresCommandContext.");
        }

        /// <summary>Inline, chained null check on <see cref="PostgresDatabase"/>.</summary>
        internal static PostgresDatabase ThrowIfNull(this PostgresDatabase src)
        {
            if (src == null)
                throw new ArgumentNullException("PostgresDatabase", "The 'PostgresDatabase' npgsqlCmdCtx is null.");
            return src;
        }

        /// <summary>Inline, chained null check on <see cref="NpgsqlParameter"/>.</summary>
        internal static NpgsqlParameter ThrowIfNull(this NpgsqlParameter src)
        {
            if (src == null)
                throw new ArgumentNullException("NpgsqlParameter", "The NpgsqlParameter instance is null.");
            return src;
        }
    }
}
