using System;
using System.Threading;

namespace contoso.sqlite.raw.recovery
{
    /// <summary>Configuration for executing a sqlite corrupt file recovery.</summary>
    public class SqliteRecoveryConfig
    {
        /// <summary>Full path to the corrupt file on which a '.dump' will be invoked.</summary>
        public string SqliteInputPath { get; set; } = default!;
        /// <summary>Full path to the output file to be created from the '.dump' output.</summary>
        public string SqliteOutputPath { get; set; } = default!;
        /// <summary>Full path to the sqlite shell.</summary>
        public string PathToSqliteShell { get; set; } = default!;
    }
}
