using System.Text.RegularExpressions;

namespace contoso.sqlite.raw
{
	internal partial class SqlWriteStatementPatterns
	{
		[GeneratedRegex(@"INSERT\s|UPDATE\s|DELETE\s", RegexOptions.IgnoreCase, "en-US")]
		public static partial Regex DataModifiers();

		[GeneratedRegex(@"INSERT\s", RegexOptions.IgnoreCase, "en-US")]
		public static partial Regex InsertToken();
	}

	/// <summary>Wraps a sqlite3 prepared statement and its parameter collection, if any.</summary>
	public sealed class WriteStatementContext : StatementContext
	{

		internal WriteStatementContext(SQLitePCL.sqlite3_stmt stmt, SqlHash sqlHash, WriteConnectionContext connectionCtx)
			: base(stmt, sqlHash, connectionCtx)
		{
			WriteConnection = connectionCtx;
			IsModifyStatement = SqlWriteStatementPatterns.DataModifiers().IsMatch(sqlHash.PreparedSql);
			IsInsertStatement = SqlWriteStatementPatterns.InsertToken().IsMatch(sqlHash.PreparedSql);
		}

		/// <summary>Get <see cref="WriteConnectionContext"/> for transaction management.</summary>
		internal WriteConnectionContext WriteConnection { get; }

		/// <summary>True if the source sql appears to be an INSERT, UPDATE or DELETE statement where rows affected should be counted.</summary>
		internal bool IsModifyStatement { get; }

		/// <summary>True if the source sql appears to be an INSERT statement where last row id might be of interest.</summary>
		internal bool IsInsertStatement { get; }

		/// <summary>Supply a delegate in which all parameters are mapped once before a single execution of the prepared sql.</summary>
		public new WriteStatementContext MapParameters(Action<BindContext> map)
		{
			MapSqlParameters = map;
			return this;
		}

		/// <summary>
		/// Supply a delegate in all parameters are mapped to their source values from each item in a collection of <typeparamref name="T"/>
		/// and the execution of the prepared sql is repeated for each item in one or more transactional batches.
		/// </summary>
		public BatchCommandContext<T> MapParameters<T>(Action<BindContext, T> map)
		{
			return new BatchCommandContext<T>(this, map);
		}

		/// <summary>Execute the prepared statement in context and return the number of rows affected.</summary>
		/// <param name="rowsAffected">Number of rows affected only if the statement is an 'INSERT', 'UPDATE', or 'DELETE'.</param>
		/// <param name="lastRowId">Last rowid if the statement is an 'INSERT'.</param>
		public void ExecuteNonQuery(out int rowsAffected, out long lastRowId)
		{
			var ctx = new CommandContext(this);
			ctx.ExecuteNonQuery(out rowsAffected, out lastRowId);
		}
	}
}
