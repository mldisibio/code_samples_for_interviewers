using System;
using System.Threading.Tasks;

namespace contoso.ado.Fluent
{
    /// <summary>
    /// Simple fluent artifact to guide command execution path when a delegate for processing
    /// each instance of <typeparamref name="T"/> is specified as part of the caller's command context.
    /// </summary>
    public sealed class DataActionOverMapTask<T>
    {
        readonly ReaderMapTaskContext<T> _asyncMappingContext;
        readonly Action<T?> _processDataObject;

        /// <summary>Initialize with the items needed to preserve the overall command context and support a fluent syntax.</summary>
        internal DataActionOverMapTask(Action<T?> processDataObject, ReaderMapTaskContext<T> asyncMappingContext)
        {
            _asyncMappingContext = asyncMappingContext;
            _processDataObject = processDataObject;
        }

        /// <summary>
        /// Returns the task from <see cref="M:NPS.AdoFramework.Common.CommandContext.ExecuteReaderAsync``1(System.Func{System.Data.Common.DbDataReader,System.Threading.Tasks.Task{``0}},System.Action{``0})"/>
        /// which applies the supplied action to each record asynchronously mapped to an instance of <typeparamref name="T"/>
        /// </summary>
        public Task ExecuteReaderAsync()
        {
            return _asyncMappingContext.CommandContext.ExecuteReaderAsync(_asyncMappingContext.MapRowAsync, _processDataObject);
        }

        /// <summary>
        /// Returns the task from <see cref="M:NPS.AdoFramework.Common.CommandContext.ExecuteScalarReaderAsync``1(System.Func{System.Data.Common.DbDataReader,System.Threading.Tasks.Task{``0}},System.Action{``0})"/>
        /// which applies the supplied action to the first record asynchronously mapped to an instance of <typeparamref name="T"/>, ignoring any other records
        /// </summary>
        public Task ExecuteScalarReaderAsync()
        {
            return _asyncMappingContext.CommandContext.ExecuteScalarReaderAsync(_asyncMappingContext.MapRowAsync, _processDataObject);
        }
    }
}
