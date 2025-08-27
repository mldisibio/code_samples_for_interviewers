using System;
using System.Collections.Generic;
using System.Linq;

namespace contoso.sqlite.raw
{
    /// <summary>
    /// The sqlite3_exec() method returns a collection of column names and a collection of column values as strings
    /// for each row returned by a sql statement. Because the sqlite3_exec() supports execution of multiple sql statements,
    /// a complete collection of <see cref="SqliteExecResult"/> from one operation may have results of different definition.
    /// </summary>
    public class SqliteExecResult
    {
        /// <summary>The collection of column names returned in the current row.</summary>
        public string[] ColumnNames { get; internal set; } = Array.Empty<string>();

        /// <summary>The collection of the string representation of column values returned in the current row.</summary>
        public string[] ColumnValues { get; internal set; } = Array.Empty<string>();

        /// <summary>Allow api to obtain a collection instance for results.</summary>
        internal static List<SqliteExecResult> CreateCollector() => new List<SqliteExecResult>();

        /// <summary>Can be used as a callback for sqlite3_exec().</summary>
        internal static int Callback(object container, string[] values, string[] names)
        {
            if (container is ICollection<SqliteExecResult> collector)
            {
                if (names.IsNotNullOrEmpty() && values.IsNotNullOrEmpty())
                    collector.Add(new SqliteExecResult { ColumnNames = names, ColumnValues = values });
            }
            return SQLitePCL.raw.SQLITE_OK;
        }
    }
}
