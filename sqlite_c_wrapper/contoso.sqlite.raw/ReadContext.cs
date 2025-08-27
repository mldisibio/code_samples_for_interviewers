namespace contoso.sqlite.raw
{
    /// <summary>Read columns returned from execution of a prepared statement into a result object.</summary>
    public sealed class ReadContext
    {
        internal ReadContext(RowContext ctx) => Row = ctx;

        /// <summary>The column metadata for all rows of the current query resultset.</summary>
        public RowContext Row { get; }

        /// <summary>Create a context for reading the value of the result column named <paramref name="colName"/>.</summary>
        public ColumnContext Read(string colName) => Read(Row[colName]);

        /// <summary>Create a context for reading the value of the result column at index <paramref name="colIdx"/>.</summary>
        public ColumnContext Read(int colIdx) => new ColumnContext(colIdx, Row);
    }
}
