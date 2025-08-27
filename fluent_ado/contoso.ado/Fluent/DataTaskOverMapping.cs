using System;
using System.Threading.Tasks;

namespace contoso.ado.Fluent
{
    /// <summary>
    /// Simple fluent artifact to guide command execution path when a task for processing
    /// each instance of <typeparamref name="T"/> is specified as part of the caller's command context.
    /// </summary>
    public sealed class DataTaskOverMapping<T>
    {
        readonly ReaderMapContext<T> _mappingContext;
        readonly Func<T?, Task> _processDataObjectAsync;

        /// <summary>Initialize with the items needed to preserve the overall command context and support a fluent syntax.</summary>
        internal DataTaskOverMapping(Func<T?, Task> processDataObjectAsync, ReaderMapContext<T> mappingContext)
        {
            _mappingContext = mappingContext;
            _processDataObjectAsync = processDataObjectAsync;
        }

        /// <summary>
        /// Returns the task from <see cref="M:NPS.AdoFramework.Common.CommandContext.ExecuteReaderAsync``1(System.Func{System.Data.Common.DbDataReader,``0},System.Func{``0,System.Threading.Tasks.Task})"/>
        /// which applies the supplied action to each record asynchronously mapped to an instance of <typeparamref name="T"/>
        /// </summary>
        public Task ExecuteReaderAsync()
        {
            return _mappingContext.CommandContext.ExecuteReaderAsync(_mappingContext.MapRow, _processDataObjectAsync);
        }

        /// <summary>
        /// Returns the task from <see cref="M:NPS.AdoFramework.Common.CommandContext.ExecuteScalarReaderAsync``1(System.Func{System.Data.Common.DbDataReader,``0},System.Func{``0,System.Threading.Tasks.Task})"/>
        /// which applies the supplied action to the first record asynchronously mapped to an instance of <typeparamref name="T"/>, ignoring any other records
        /// </summary>
        public Task ExecuteScalarReaderAsync()
        {
            return _mappingContext.CommandContext.ExecuteScalarReaderAsync(_mappingContext.MapRow, _processDataObjectAsync);
        }
    }
}
