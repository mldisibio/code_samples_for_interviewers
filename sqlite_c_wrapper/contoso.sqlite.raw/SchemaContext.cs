using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using contoso.sqlite.raw.stringutils;

namespace contoso.sqlite.raw
{
    /// <summary>
    /// Provides a lookup of the database (or databases if others were attached), tables, views, and columns available to the current database connection.
    /// </summary>
    public class SchemaContext
    {
        internal const string DbListQuery = "SELECT name, file FROM pragma_database_list WHERE name != 'temp';";
        readonly Dictionary<string, DbSchema> _schemas;

        SchemaContext()
        {
            _schemas = new Dictionary<string, DbSchema>(StringComparer.OrdinalIgnoreCase);
            Schemas = new ReadOnlyDictionary<string, DbSchema>(_schemas);
        }

        /// <summary>A lookup of the database (or databases if others were attached), tables, views, and columns available to the current database connection.</summary>
        public IReadOnlyDictionary<string, DbSchema> Schemas { get; }

        /// <summary>True if the database <paramref name="schemaName"/> is attached to the current connection.</summary>
        public bool HasSchema(string schemaName) => schemaName.IsNotNullOrEmptyString() && _schemas.ContainsKey(schemaName);

        /// <summary>Returns the <see cref="DbSchema"/> <paramref name="schemaName"/> if attached to the current connection, otherwise null.</summary>
        public DbSchema? this[string schemaName] => HasSchema(schemaName) ? _schemas[schemaName] : null;

        /// <summary>True if the database <paramref name="schemaName"/> is attached to the current connection, otherwise false.</summary>
        public bool TryGetSchema(string schemaName, out DbSchema? dbSchema)
        {
            dbSchema = null;
            return HasSchema(schemaName) && _schemas.TryGetValue(schemaName, out dbSchema);
        }

        internal void AddOrReplaceDb(DbSchema schema)
        {
            if (schema?.Name != null)
                _schemas[schema.Name] = schema;
        }

        internal static SchemaContext CreateFrom(ConnectionContext readCtx)
        {
            readCtx.DbHandle.ThrowIfInvalid();
            var currentSchema = new SchemaContext();
            // get all databases attached to current connection; this would be 'main', 'temp' if any, and any files where the ATTACH command was invoked
            var dbList = readCtx.GetOrPrepare(DbListQuery)
                                .MapRow(row => DbSchema.Map(row))
                                .ExecuteReader();

            foreach (DbSchema? attachedDb in dbList)
            {
                currentSchema.AddOrReplaceDb(attachedDb!);
                // get all tables in (each) database
                var tableList = readCtx.GetOrPrepare(attachedDb!.TablesQuery)
                                       .MapRow(row => DbTable.Map(row, attachedDb.Name))
                                       .ExecuteReader();
                foreach (DbTable? tbl in tableList)
                {
                    attachedDb.AddOrReplaceTable(tbl!);
                    // get all columns for (each) table
                    var columnList = readCtx.GetOrPrepare(tbl!.ColumnsQuery)
                                            .MapRow(row => DbColumn.Map(row))
                                            .ExecuteReader();
                    foreach (DbColumn? col in columnList)
                    {
                        tbl.AddOrReplaceColumn(col!);
                    }
                }
                // get all views in (each) database
                var viewList = readCtx.GetOrPrepare(attachedDb.ViewsQuery)
                                      .MapRow(row => DbView.Map(row))
                                      .ExecuteReader();
                foreach (DbView? vw in viewList)
                {
                    attachedDb.AddOrReplaceView(vw!);
                    // get all columns for (each) view
                    var columnList = readCtx.GetOrPrepare(vw!.ColumnsQuery)
                                            .MapRow(row => DbColumn.Map(row))
                                            .ExecuteReader();
                    foreach (DbColumn? col in columnList)
                    {
                        vw.AddOrReplaceColumn(col!);
                    }
                }
            }
            return currentSchema;
        }
    }

    /// <summary>A lookup of tables, views, and columns found in a given database schema.</summary>
    public class DbSchema
    {
        int? _hash;
        string? _display;
        readonly Dictionary<string, DbTable> _tables;
        readonly Dictionary<string, DbView> _views;

        internal DbSchema()
        {
            _tables = new Dictionary<string, DbTable>(StringComparer.OrdinalIgnoreCase);
            _views = new Dictionary<string, DbView>(StringComparer.OrdinalIgnoreCase);
            Tables = new ReadOnlyDictionary<string, DbTable>(_tables);
            Views = new ReadOnlyDictionary<string, DbView>(_views);
        }

        internal string TablesQuery => $"SELECT name, sql FROM {Name}.sqlite_master WHERE type = 'table';";

        internal string ViewsQuery => $"SELECT name, sql FROM {Name}.sqlite_master WHERE type = 'view';";

        /// <summary>The name of the schema. This will be 'main', 'temp' if any, and any files where the ATTACH command was invoked</summary>
        public string Name { get; internal set; } = default!;

        /// <summary>The database file path, or null if not associated with a file.</summary>
        public string? FilePath { get; internal set; }

        /// <summary>The lookup of objects of type 'table' associated with this schema.</summary>
        public IReadOnlyDictionary<string, DbTable> Tables { get; }

        /// <summary>The lookup of objects of type 'view' associated with this schema.</summary>
        public IReadOnlyDictionary<string, DbView> Views { get; }

        /// <summary>True if <paramref name="tableName"/> is defined as a table in the current schema.</summary>
        public bool HasTable(string tableName) => tableName.IsNotNullOrEmptyString() && _tables.ContainsKey(tableName);

        /// <summary>True if <paramref name="viewName"/> is defined as a view in the current schema.</summary>
        public bool HasView(string viewName) => viewName.IsNotNullOrEmptyString() && _views.ContainsKey(viewName);

        /// <summary>True if <paramref name="objName"/> is defined as a table or view in the current schema.</summary>
        public bool HasTableOrView(string objName) => HasTable(objName) || HasView(objName);

        /// <summary>The collection of object names of type 'table'.</summary>
        public IEnumerable<string> TableNames => _tables.Keys.Cast<string>();

        /// <summary>The collection of object names of type 'view'.</summary>
        public IEnumerable<string> ViewNames => _views.Keys.Cast<string>();

        /// <summary>The collection of object names of type 'table' or of type 'view.</summary>
        public IEnumerable<string> TableAndViewNames => TableNames.Concat(ViewNames);

        /// <summary>Returns the table or view <paramref name="objName"/> as <see cref="DbTable"/> if defined in the current schema, otherwise null.</summary>
        public DbTable? this[string objName] => HasTable(objName) ? _tables[objName] : HasView(objName) ? _views[objName] : null;

        /// <summary>True if <paramref name="tableName"/> is defined in the current schema as a 'table' type, otherwise false.</summary>
        public bool TryGetTable(string tableName, out DbTable? dbTable)
        {
            dbTable = null;
            return HasTable(tableName) && _tables.TryGetValue(tableName, out dbTable);
        }

        /// <summary>True if <paramref name="viewName"/> is defined in the current schema as a 'view' type, otherwise false.</summary>
        public bool TryGetView(string viewName, out DbView? dbView)
        {
            dbView = null;
            return HasView(viewName) && _views.TryGetValue(viewName, out dbView);
        }

        /// <summary>True if <paramref name="objName"/> is defined in the current schema as a 'table' or 'view' type, otherwise false.</summary>
        public bool TryGetTableOrView(string objName, out DbTable? dbTable)
        {
            if (TryGetTable(objName, out dbTable))
                return true;
            else if (TryGetView(objName, out DbView? vw))
            {
                dbTable = vw;
                return true;
            }
            return false;
        }

        internal void AddOrReplaceTable(DbTable tbl)
        {
            if (tbl?.Name != null)
                _tables[tbl.Name] = tbl;
        }

        internal void AddOrReplaceView(DbView view)
        {
            if (view?.Name != null)
                _views[view.Name] = view;
        }

        internal static DbSchema Map(ReadContext row)
        {
            return new DbSchema
            {
                Name = row.Read("name").AsString()!,
                FilePath = row.Read("file").AsString()!,
            };
        }

        /// <summary>Display string.</summary>
        public override string ToString()
        {
            if (_display == null && Name.IsNotNullOrEmptyString())
                _display = $"{Name} [{FilePath ?? "memory"}]";
            return _display!;
        }

        /// <summary>Equality hash.</summary>
        public override int GetHashCode()
        {
            if (!_hash.HasValue && Name != null)
                _hash = Name.ToLower().GetHashCode();
            return _hash.GetValueOrDefault();
        }

        /// <summary>True if the names are case-insensitive equal.</summary>
        public override bool Equals(object? obj) => Equals(obj as DbSchema);

        /// <summary>True if the names are case-insensitive equal.</summary>
        public bool Equals(DbSchema? other)
        {
            if (other == null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>Metadata for an object of type 'table' and a lookup of its columns.</summary>
    public class DbTable : IEquatable<DbTable>
    {
        int? _hash;
        string? _display;
        readonly Dictionary<string, DbColumn> _columns;

        internal DbTable(string schemaName = "main")
        {
            Schema = schemaName;
            _columns = new Dictionary<string, DbColumn>(StringComparer.OrdinalIgnoreCase);
            Columns = new ReadOnlyDictionary<string, DbColumn>(_columns);
        }

        internal string ColumnsQuery => $"PRAGMA {Schema}.table_info({Name});";

        /// <summary>The name of the parent schema. This will be 'main', 'temp' if any, and any files where the ATTACH command was invoked</summary>
        public string Schema { get; }

        /// <summary>The table name.</summary>
        public string Name { get; internal set; } = default!;

        /// <summary>The normalized sql used to create and/or alter the table.</summary>
        public string Sql { get; internal set; } = default!;

        /// <summary>The table name qualified by its schema name.</summary>
        public string QualifiedName => Schema.IsNullOrEmptyString() ? $"main.{Name}" : $"{Schema}.{Name}";

        /// <summary>The lookup of columns making up the table definition.</summary>
        public IReadOnlyDictionary<string, DbColumn> Columns { get; }

        /// <summary>True if <paramref name="columnName"/> is defined as a column in the current table.</summary>
        public bool HasColumn(string columnName) => columnName.IsNotNullOrEmptyString() && _columns.ContainsKey(columnName);

        /// <summary>True if <paramref name="columnName"/> is defined in the current table, otherwise false.</summary>
        public bool TryGetColumn(string columnName, out DbColumn? dbColumn)
        {
            dbColumn = null;
            return HasColumn(columnName) && _columns.TryGetValue(columnName, out dbColumn);
        }

        /// <summary>Returns the <see cref="DbColumn"/> <paramref name="columnName"/> if defined in the current table, otherwise null.</summary>
        public DbColumn? this[string columnName] => HasColumn(columnName) ? _columns[columnName] : null;

        /// <summary>The column names as a collection of string.</summary>
        public IEnumerable<string> ColumnNames => _columns.Keys.Cast<string>();

        /// <summary>
        /// Returns the set of column names from this table that also appear in <paramref name="otherColumns"/>.
        /// This enables a SELECT statement to span versions of the same table that may have a few columns added or removed.
        /// </summary>
        public IEnumerable<string> ColumnIntersectionWith(IEnumerable<string> otherColumns) => ColumnNames.Intersect(otherColumns.EmptyIfNull(), StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Returns the set of column names from this table that also appear in <paramref name="otherTable"/>.
        /// This enables a SELECT statement to span versions of the same table that may have a few columns added or removed.
        /// </summary>
        public IEnumerable<string> ColumnIntersectionWith(DbTable otherTable) => ColumnNames.Intersect(otherTable.ColumnNames, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Produce a comma separated list from <paramref name="columns"/> that can be used in a SELECT statement.
        /// Returns an empty string if <paramref name="columns"/> is null.
        /// </summary>
        /// <param name="columns">The collection of column names.</param>
        /// <param name="toLower">True if all items should be converted to lower case. Default is to leave as-is.</param>
        public static string CsvOfColumnNames(IEnumerable<string> columns, bool toLower = false) => columns.AsDelimitedString(delim: ",", toLower: toLower);

        internal void AddOrReplaceColumn(DbColumn col)
        {
            if (col?.Name != null)
                _columns[col.Name] = col;
        }

        internal static DbTable Map(ReadContext row, string schemaName = "main")
        {
            return new DbTable(schemaName)
            {
                Name = row.Read("name").AsString()!,
                Sql = row.Read("sql").AsString()!,
            };
        }

        /// <summary>Display string.</summary>
        public override string ToString()
        {
            if (_display == null && Name.IsNotNullOrEmptyString())
                _display = $"{QualifiedName} ({_columns.Count} columns)";
            return _display!;
        }

        /// <summary>Equality hash.</summary>
        public override int GetHashCode()
        {
            if (!_hash.HasValue && Name != null)
                _hash = QualifiedName.ToLower().GetHashCode();
            return _hash.GetValueOrDefault();
        }

        /// <summary>True if the schema qualified names are case-insensitive equal.</summary>
        public override bool Equals(object? obj) => Equals(obj as DbTable);

        /// <summary>True if the schema qualified names are case-insensitive equal.</summary>
        public bool Equals(DbTable? other)
        {
            if (other == null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return string.Equals(this.QualifiedName, other.QualifiedName, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Metadata for an object of type 'view' and a lookup of its columns.
    /// A <see cref="DbView"/> derives from <see cref="DbTable"/> and has the same exact behaviors and properties.
    /// </summary>
    public class DbView : DbTable
    {
        internal DbView(string schemaName) : base(schemaName) { }

        internal static new DbView Map(ReadContext row, string schemaName = "main")
        {
            return new DbView(schemaName)
            {
                Name = row.Read("name").AsString()!,
                Sql = row.Read("sql").AsString()!,
            };
        }
    }

    /// <summary>Metadata for a single column from a given table or view.</summary>
    public class DbColumn : IEquatable<DbColumn>
    {
        int? _hash;
        string? _display;

        internal DbColumn() { }

        /// <summary>The column name.</summary>
        public string Name { get; internal set; } = default!;

        /// <summary>The column id, which is also its position in the table.</summary>
        public int ColumnId { get; internal set; }

        /// <summary>The storage type as listed in the 'sqlite_master' table.</summary>
        public string StorageType { get; internal set; } = default!;

        /// <summary>True if the column has a not null constraint. Note that a primary key is not implicitly set to not null.</summary>
        public bool NotNull { get; internal set; }

        /// <summary>The index of this column in the primary key definition (usually '1' unless it is a composite primary key), or null if not part of a primary key.</summary>
        public int? PKIndex { get; internal set; }

        /// <summary>True if this column is or is part of the primary key.</summary>
        public bool IsPK => PKIndex.HasValue;

        internal static DbColumn Map(ReadContext row)
        {
            int pkIdx = row.Read("pk").AsInt32();
            return new DbColumn
            {
                ColumnId = row.Read("cid").AsInt32(),
                Name = row.Read("name").AsString()!,
                StorageType = row.Read("type").AsString()!,
                NotNull = row.Read("notnull").AsBoolean(),
                // zero if not part of PK; otherwise, column index within PK
                PKIndex = pkIdx == 0 ? null : pkIdx
            };
        }

        /// <summary>Display string.</summary>
        public override string ToString()
        {
            if (_display == null && Name.IsNotNullOrEmptyString())
                _display = $"{ColumnId} {Name} {StorageType}";
            return _display!;
        }

        /// <summary>Equality hash.</summary>
        public override int GetHashCode()
        {
            if (!_hash.HasValue && Name != null)
                _hash = Name.ToLower().GetHashCode();
            return _hash.GetValueOrDefault();
        }

        /// <summary>True if the names are case-insensitive equal.</summary>
        public override bool Equals(object? obj) => Equals(obj as DbColumn);

        /// <summary>True if names are case-insensitive equal.</summary>
        public bool Equals(DbColumn? other)
        {
            if (other == null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
