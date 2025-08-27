using System;
using System.IO;

namespace contoso.sqlite.raw
{
    /// <summary>Wraps and formats the integer result, if any, and the error message, if any, from a failed native sqlite operation.</summary>
    public class SqliteDbException : Exception
    {
        /// <summary>Initialize with the sqlite result code, error message, database identifier, and any existing exception.</summary>
        public SqliteDbException(int? result, string? msg, string? filePath = null, Exception? innerException = null)
            : base(msg, innerException)
        {
            ResultCode = result;
            if (filePath.IsNotNullOrEmptyString())
                FilePath = filePath;
        }

        /// <summary>The native sqlite3 result code.</summary>
        public int? ResultCode { get; }

        /// <summary>Full path to the database file.</summary>
        public string? FilePath { get; }

        /// <summary>Short name of the database file.</summary>
        public string? FileName => FilePath == null ? null : Path.GetFileName(FilePath);

        /// <summary>String representation of the error message.</summary>
        public override string ToString()
        {
            bool noMsg = Message.IsNullOrEmptyString();
            bool noPath = FilePath == null;
            if (noMsg && noPath)
                return "Undefined error";
            string sp = noMsg || noPath ? string.Empty : " ";
            string fn = noPath ? string.Empty : $"[{FileName}]";

            return $"{Message}{sp}{fn}";
        }
    }

    /// <summary>Decorates an exception as coming from a 'BEGIN', 'COMMIT', or 'ROLLBACK' statement.</summary>
    public class SqliteTransactionException : SqliteDbException
    {
        /// <summary>Initialize from an existing <see cref="SqliteDbException"/> generated from an error code.</summary>
        public SqliteTransactionException(SqliteDbException src, string transactionSql)
            : base(src.ResultCode, src.Message, src.FilePath, src.InnerException)
        {
            Statement = transactionSql;
        }

        /// <summary>Initialize from a runtime exception.</summary>
        public SqliteTransactionException(Exception src, string transactionSql, string? filePath = null)
            : base(ResultCodes.NonSqliteException, src.Message, filePath, src)
        {
            Statement = transactionSql;
        }

        /// <summary>The transaction verb which failed ('BEGIN', 'COMMIT', or 'ROLLBACK').</summary>
        public string Statement { get; }
    }
}
