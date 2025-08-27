using System;
using System.Data.Common;
using contoso.ado.Common;
using contoso.ado.Internals;

namespace contoso.ado.Fluent
{
    /// <summary>Fluent artifact that serves as the starting point and overall conceptual context for a database operation.</summary>
    public sealed class AdoTransactionContext
    {
        readonly Database _db;
        readonly TransactionContext _ctx;

        internal AdoTransactionContext(Database dbContext, TransactionContext transactionCtx)
        {
            _db = dbContext.ThrowIfNull();
            ParamCheck.Assert.IsNotNull(transactionCtx, "AdoTransactionContext");
            _ctx = transactionCtx;
        }

        /// <summary>Returns the context for executing a <see cref="DbCommand"/> created from a SQL query string.</summary>
        /// <param name="queryString">The text of the query.</param>        
        public EnlistedCommandContext FromSqlString(string queryString)
        {
            DbCommand cmd = _db.CreateCommandFromSqlString(queryString);
            return _db.CreateTransactionCommandContext(cmd, _ctx);
        }

        /// <summary>Returns the context for executing a stored procedure <see cref="DbCommand"/>.</summary>
        /// <param name="sprocName">The stored procedure name.</param>
        public EnlistedCommandContext FromSprocName(string sprocName)
        {
            DbCommand cmd = _db.CreateCommandFromSprocName(sprocName);
            return _db.CreateTransactionCommandContext(cmd, _ctx);
        }

        /// <summary>
        /// Returns the context for iteratively executing a <see cref="DbCommand"/> created from a SQL query string
        /// as a bulk operation wrapped in a single transaction, for each item in a collection of <typeparamref name="T"/>.
        /// </summary>
        /// <param name="queryString">The text of the query.</param>        
        public EnlistedBulkOperationContext<T> FromBulkSqlString<T>(string queryString)
        {
            DbCommand cmd = _db.CreateCommandFromSqlString(queryString);
            return _db.CreateBulkOperationCommandContext<T>(cmd, _ctx);
        }

        /// <summary>
        /// Returns the context for iteratively executing a stored procedure <see cref="DbCommand"/>
        /// as a bulk operation wrapped in a single transaction, for each item in a collection of <typeparamref name="T"/>.
        /// </summary>
        /// <param name="sprocName">The stored procedure name.</param>
        public EnlistedBulkOperationContext<T> FromBulkSprocName<T>(string sprocName)
        {
            DbCommand cmd = _db.CreateCommandFromSprocName(sprocName);
            return _db.CreateBulkOperationCommandContext<T>(cmd, _ctx);
        }
    }
}