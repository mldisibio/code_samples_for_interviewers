using System;
using System.Threading.Tasks;
using System.Xml;
using contoso.ado.Common;
using contoso.ado.DataActions;
using contoso.ado.Internals;

namespace contoso.ado.Fluent
{
    /// <summary>
    /// Simple fluent artifact to guide command execution path when a delegate for mapping an
    /// <see cref="XmlReader"/> record is specified as part of the caller's command context.
    /// </summary>
    public sealed class XmlReaderMapContext<TOut>
    {
        internal readonly CommandContext CommandContext;
        internal readonly Func<XmlReader, TOut> MapXml;

        /// <summary>Initialize with the items needed to preserve the overall command context and support a fluent syntax.</summary>
        internal XmlReaderMapContext(Func<XmlReader, TOut> mapXml, CommandContext cmdContext)
        {
            CommandContext = cmdContext;
            MapXml = mapXml;
        }

        /// <summary>
        /// Invokes the <paramref name="processDataObject"/> delegate for each instance of <typeparamref name="TOut"/>
        /// mapped while iterating the <see cref="XmlReader"/>.
        /// </summary>
        public XmlActionOverMapping<TOut> WithDataAction(Action<TOut> processDataObject)
        {
            processDataObject ??= DataAction.DoNothing<TOut>;
            return new XmlActionOverMapping<TOut>(processDataObject, this);
        }

        /// <summary>
        /// Awaits the <paramref name="processDataObjectAsync"/> task for each instance of <typeparamref name="TOut"/>
        /// mapped while iterating the <see cref="XmlReader"/>.
        /// </summary>
        public XmlTaskOverMapping<TOut> WithDataTask(Func<TOut, Task> processDataObjectAsync)
        {
            ParamCheck.Assert.IsNotNull(processDataObjectAsync, "processDataObjectAsync");
            return new XmlTaskOverMapping<TOut>(processDataObjectAsync, this);
        }

        /// <summary>
        /// Executes the <see cref="System.Data.Common.DbCommand"/> in context, maps and returns the content of the <see cref="XmlReader"/> as an instance of <typeparamref name="TOut"/>.
        /// </summary>
        public TOut ExecuteXmlReader() => CommandContext.ExecuteXmlReader(MapXml);

        /// <summary>
        /// Returns a task which executes the <see cref="System.Data.Common.DbCommand"/> in context, maps and returns the content of the <see cref="XmlReader"/> as an instance of <typeparamref name="TOut"/>.
        /// </summary>
        public Task<TOut> ExecuteXmlReaderAsync() => CommandContext.ExecuteXmlReaderAsync(MapXml);

    }
}
