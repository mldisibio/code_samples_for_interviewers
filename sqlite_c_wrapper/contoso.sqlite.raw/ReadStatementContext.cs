using System;

namespace contoso.sqlite.raw
{
    /// <summary>Wraps a sqlite3 prepared statement and its parameter collection, if any.</summary>
    public sealed class ReadStatementContext : StatementContext
    {
        internal ReadStatementContext(SQLitePCL.sqlite3_stmt stmt, SqlHash sqlHash, ReadConnectionContext connectionCtx)
            : base(stmt, sqlHash, connectionCtx)
        { }

        /// <summary>Supply a delegate in which all parameters are mapped once before a single execution of the prepared sql.</summary>
        public new ReadStatementContext MapParameters(Action<BindContext> map)
        {
            MapSqlParameters = map;
            return this;
        }
    }
}
