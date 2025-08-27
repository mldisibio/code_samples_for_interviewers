using System;
using System.Threading.Tasks;

namespace contoso.ado.Fluent
{
    /// <summary>
    /// Simple fluent artifact to guide command execution path when a task for processing
    /// each instance of <typeparamref name="T"/> is specified as part of the caller's command context.
    /// </summary>
    public sealed class XmlTaskOverMapping<T>
    {
        readonly XmlReaderMapContext<T> _mappingContext;
        readonly Func<T, Task> _processDataObjectAsync;

        /// <summary>Initialize with the items needed to preserve the overall command context and support a fluent syntax.</summary>
        internal XmlTaskOverMapping(Func<T, Task> processDataObjectAsync, XmlReaderMapContext<T> mappingContext)
        {
            _mappingContext = mappingContext;
            _processDataObjectAsync = processDataObjectAsync;
        }

        /// <summary>
        /// Returns the task from <see cref="M:NPS.AdoFramework.Common.CommandContext.ExecuteXmlReaderAsync``1(System.Func{System.Xml.XmlReader,``0},System.Func{``0,System.Threading.Tasks.Task})"/>
        /// which applies the supplied action to each record asynchronously mapped to an instance of <typeparamref name="T"/>
        /// </summary>
        public Task ExecuteXmlReaderAsync() => _mappingContext.CommandContext.ExecuteXmlReaderAsync(_mappingContext.MapXml, _processDataObjectAsync);
    }
}
