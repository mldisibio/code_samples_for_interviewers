using System;
using System.Threading.Tasks;

namespace contoso.ado.Fluent
{
    /// <summary>
    /// Simple fluent artifact to guide command execution path when a delegate for processing
    /// each instance of <typeparamref name="T"/> is specified as part of the caller's command context.
    /// </summary>
    public sealed class DataActionOverMapping<T>
    {
        readonly ReaderMapContext<T> _mappingContext;
        readonly Action<T?> _processDataObject;

        /// <summary>Initialize with the items needed to preserve the overall command context and support a fluent syntax.</summary>
        internal DataActionOverMapping(Action<T?> processDataObject, ReaderMapContext<T> mappingContext)
        {
            _mappingContext = mappingContext;
            _processDataObject = processDataObject;
        }

        /// <summary>
        /// Invokes <see cref="M:NPS.AdoFramework.Common.CommandContext.ExecuteReader``1(System.Func{System.Data.Common.DbDataReader,``0},System.Action{``0})"/>
        /// which applies the supplied action to each record mapped to an instance of <typeparamref name="T"/>
        /// </summary>
        public void ExecuteReader()
        {
            _mappingContext.CommandContext.ExecuteReader(_mappingContext.MapRow, _processDataObject);
        }

        /// <summary>
        /// Returns the task from <see cref="M:NPS.AdoFramework.Common.CommandContext.ExecuteReaderAsync``1(System.Func{System.Data.Common.DbDataReader,``0},System.Action{``0})"/>
        /// which applies the supplied action to each record mapped to an instance of <typeparamref name="T"/>
        /// </summary>
        public Task ExecuteReaderAsync()
        {
            return _mappingContext.CommandContext.ExecuteReaderAsync(_mappingContext.MapRow, _processDataObject);
        }

        /// <summary>
        /// Invokes <see cref="M:NPS.AdoFramework.Common.CommandContext.ExecuteScalarReader``1(System.Func{System.Data.Common.DbDataReader,``0},System.Action{``0})"/>
        /// which applies the supplied action to the first record mapped to an instance of <typeparamref name="T"/>, ignoring any other records.
        /// </summary>
        public void ExecuteScalarReader()
        {
            _mappingContext.CommandContext.ExecuteScalarReader(_mappingContext.MapRow, _processDataObject);
        }

        /// <summary>
        /// Returns the task from <see cref="M:NPS.AdoFramework.Common.CommandContext.ExecuteScalarReaderAsync``1(System.Func{System.Data.Common.DbDataReader,``0},System.Action{``0})"/>
        /// which applies the supplied action to the first record mapped to an instance of <typeparamref name="T"/>, ignoring any other records.
        /// </summary>
        public Task ExecuteScalarReaderAsync()
        {
            return _mappingContext.CommandContext.ExecuteScalarReaderAsync(_mappingContext.MapRow, _processDataObject);
        }
    }
}
