using System;
using System.Threading.Tasks;

namespace contoso.ado.Fluent
{
    /// <summary>
    /// Simple fluent artifact to guide command execution path when a delegate for processing
    /// each instance of <typeparamref name="T"/> is specified as part of the caller's command context.
    /// </summary>
    public sealed class XmlActionOverMapTask<T>
    {
        readonly XmlReaderMapTaskContext<T> _asyncMappingContext;
        readonly Action<T> _processDataObject;

        /// <summary>Initialize with the items needed to preserve the overall command context and support a fluent syntax.</summary>
        internal XmlActionOverMapTask(Action<T> processDataObject, XmlReaderMapTaskContext<T> asyncMappingContext)
        {
            _asyncMappingContext = asyncMappingContext;
            _processDataObject = processDataObject;
        }

        /// <summary>
        /// Returns the task from <see cref="M:NPS.AdoFramework.Common.CommandContext.ExecuteXmlReaderAsync``1(System.Func{System.Xml.XmlReader,System.Threading.Tasks.Task{``0}},System.Action{``0})"/>
        /// which applies the supplied action to each record asynchronously mapped to an instance of <typeparamref name="T"/>
        /// </summary>
        public Task ExecuteXmlReaderAsync() => _asyncMappingContext.CommandContext.ExecuteXmlReaderAsync(_asyncMappingContext.MapXmlAsync, _processDataObject);
    }
}
