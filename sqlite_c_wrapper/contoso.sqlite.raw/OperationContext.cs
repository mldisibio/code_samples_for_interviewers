using System;
using System.Runtime.CompilerServices;
using contoso.sqlite.raw.stringutils;

namespace contoso.sqlite.raw
{

    /// <summary>Accessibility to the open connection, file path, and prepared statement in context.</summary>
    public abstract class OperationContext : SqliteContext
    {
        /// <summary>Intialize with a new prepared statement, the sql string for that statement, an open sqlite connection and the identifier of the opened file.</summary>
        protected OperationContext(SQLitePCL.sqlite3_stmt stmt, SqlHash sqlHash, SQLitePCL.sqlite3 dbHandle, string filePath)
            : base(dbHandle, filePath)
        {
            Statement = stmt;
            SqlHash = sqlHash;
        }

        /// <summary>Intialize with a new prepared statement, the sql string for that statement, and an existing <see cref="SqliteContext"/>.</summary>
        protected OperationContext(SQLitePCL.sqlite3_stmt stmt, SqlHash sqlHash, SqliteContext ctx)
            : base(ctx)
        {
            Statement = stmt;
            SqlHash = sqlHash;
        }

        /// <summary>Intialize as an existing <see cref="OperationContext"/></summary>
        protected OperationContext(OperationContext ctx)
            : this(ctx.Statement, ctx.SqlHash, ctx) { }

        /// <summary>A prepared sqlite3 sql statement.</summary>
        internal SQLitePCL.sqlite3_stmt Statement { get; }

        /// <summary>A wrapper around the raw sql from which the sqlite3 statement was prepared, allowing it to be uniquely identified.</summary>
        internal SqlHash SqlHash { get; }

        /// <summary>The sql string from which the sqlite3 statement was prepared.</summary>
        public string Sql => SqlHash.PreparedSql;

        /// <summary>The first 256 chars of <see cref="Sql"/>, flattened.</summary>
        public string SqlAbbrev => Sql.NewlinesToSpace(includeTabs:true).TrimConsecutiveSpaces().Left(256).ToString();

        /// <summary>
        /// Create a <see cref="SqliteDbException"/> from an error code retured by a statement execution.
        /// Will log the failed execution and invoke 'sqlite3_reset' on the statement.
        /// </summary>
        protected SqliteDbException FailedExecution(int resultCode, Exception? innerException = null, [CallerMemberName] string methodName = "")
        {
            try
            {
                // retrieve error message
                string errMsg = DbHandle.SeemsValid()
                                ? SqliteDatabase.TryRetrieveError(base.DbHandle, resultCode)
                                : $"{resultCode}-{ResultCodes.Lookup[resultCode]}";
                // capture failed sql
                string debugSql = Statement.SeemsValid()
                                  ? SQLitePCL.raw.sqlite3_sql(Statement).utf8_to_string()
                                  : Sql;
                // compose exception
                var cmdEx = new SqliteDbException(result: resultCode, msg: $"From {methodName}: {errMsg}", filePath: FilePath, innerException: innerException);
                // log failure
                SqliteDatabase.LogQueue.CommandFailed(debugSql, cmdEx);
                return cmdEx;
            }
            finally
            {
                // attempt to reset the statement
                ResetStatement();
            }
        }

        /// <summary>
        /// Reset a prepared execution statement. Sets <see cref="Statement"/> back to its initial state, ready to be re-executed.
        /// Should be invoked if a prepared statement is to be re-used after it has been executed and all result rows processed.
        /// Does not, however, reset any bound variables.
        /// </summary>
        public void ResetStatement()
        {
            // since sqlite3_reset() normally returns SQLITE_OK but also returns the last error from sqlite3_step() if any
            // it would not make sense to evaluate its return code after evaluation sqlite3_step()
            if (Statement.SeemsValid())
                SQLitePCL.raw.sqlite3_reset(Statement);

        }

        /// <summary>Clean up resources.</summary>
        protected override void Dispose(bool isManagedCall)
        {
            base.Dispose(isManagedCall);
        }
    }
}
