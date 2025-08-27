using System;
using System.Data.Common;
using contoso.ado.Common;
using contoso.ado.Internals;

namespace contoso.ado.Fluent
{
    /// <summary>Fluent artifact that serves as the starting point and overall conceptual context for a database operation.</summary>
    public sealed class AdoContext
    {
        readonly Database _db;

        internal AdoContext(Database dbContext)
        {
            _db = dbContext.ThrowIfNull();
        }

        /// <summary>Returns the context for executing a <see cref="DbCommand"/> created from a SQL query string.</summary>
        /// <param name="queryString">The text of the query.</param>        
        public CommandContext FromSqlString(string queryString)
        {
            DbCommand cmd = _db.CreateCommandFromSqlString(queryString);
            return _db.CreateCommandContext(cmd);
        }

        /// <summary>Returns the context for executing a stored procedure <see cref="DbCommand"/>.</summary>
        /// <param name="sprocName">The stored procedure name.</param>
        public CommandContext FromSprocName(string sprocName)
        {
            DbCommand cmd = _db.CreateCommandFromSprocName(sprocName);
            return _db.CreateCommandContext(cmd);
        }
    }
}
