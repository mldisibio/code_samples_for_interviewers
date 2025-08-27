using System;

namespace contoso.sqlite.raw
{
    /// <summary>Allows a sql statement to be used as a lookup key.</summary>
    public class SqlHash : IEquatable<SqlHash>
    {
        int? _hash;

        internal SqlHash(string sql)
        {
            Sql = sql;
            PreparedSql = sql;
        }

        internal SqlHash(string sql, string? remainingSql)
            :this(sql)
        {
            PreparedSql = remainingSql.IsNullOrEmptyString() ? sql : sql.Replace(remainingSql, string.Empty);
        }

        /// <summary>The original, application supplied sql statement. May contain parameter placeholders.</summary>
        public string Sql { get; }

        /// <summary>The actual sql parsed by sqlite3_prepare_v2(). For multi-statement text, only the first complete statement is parsed.</summary>
        public string PreparedSql { get; internal set; }

        /// <summary>The hash of <see cref="Sql"/>.</summary>
        public override int GetHashCode()
        {
            if (_hash.GetValueOrDefault() == 0)
                _hash = Sql.GetHashCode();
            return _hash!.Value;
        }

        /// <summary>Returns <see cref="Sql"/>.</summary>
        public override string ToString() => Sql;

        /// <summary>True if two sql strings are case-insensitive equal.</summary>
        public override bool Equals(object? obj) => Equals(obj as SqlHash);

        /// <summary>True if two sql strings are case-insensitive equal.</summary>
        public bool Equals(SqlHash? other)
        {
            if (other == null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return string.Equals(this.Sql, other.Sql, StringComparison.OrdinalIgnoreCase);
        }
    }
}
