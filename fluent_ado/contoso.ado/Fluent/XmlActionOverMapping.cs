using System;
using System.Threading.Tasks;

namespace contoso.ado.Fluent
{
    /// <summary>
    /// Simple fluent artifact to guide command execution path when a delegate for processing
    /// each instance of <typeparamref name="T"/> is specified as part of the caller's command context.
    /// </summary>
    public sealed class XmlActionOverMapping<T>
    {
        readonly XmlReaderMapContext<T> _mappingContext;
        readonly Action<T> _processDataObject;

        /// <summary>Initialize with the items needed to preserve the overall command context and support a fluent syntax.</summary>
        internal XmlActionOverMapping(Action<T> processDataObject, XmlReaderMapContext<T> mappingContext)
        {
            _mappingContext = mappingContext;
            _processDataObject = processDataObject;
        }

        /// <summary>
        /// Invokes <see cref="M:NPS.AdoFramework.Common.CommandContext.ExecuteXmlReader``1(System.Func{System.Xml.XmlReader,``0},System.Action{``0})"/>
        /// which applies the supplied action to the xml content mapped to an instance of <typeparamref name="T"/>
        /// </summary>
        public void ExecuteXmlReader() => _mappingContext.CommandContext.ExecuteXmlReader(_mappingContext.MapXml, _processDataObject);

        /// <summary>
        /// Returns the task from <see cref="M:NPS.AdoFramework.Common.CommandContext.ExecuteXmlReaderAsync``1(System.Func{System.Xml.XmlReader,``0},System.Action{``0})"/>
        /// which applies the supplied action to the xml content mapped to an instance of <typeparamref name="T"/>
        /// </summary>
        public Task ExecuteXmlReaderAsync() => _mappingContext.CommandContext.ExecuteXmlReaderAsync(_mappingContext.MapXml, _processDataObject);
    }
}
