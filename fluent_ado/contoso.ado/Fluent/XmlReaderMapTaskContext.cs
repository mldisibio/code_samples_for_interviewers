using System;
using System.Threading.Tasks;
using System.Xml;
using contoso.ado.Common;
using contoso.ado.DataActions;
using contoso.ado.Internals;

namespace contoso.ado.Fluent
{
    /// <summary>
    /// Simple fluent artifact to guide command execution path when a task for mapping an
    /// <see cref="XmlReader"/> record is specified as part of the caller's command context.
    /// </summary>
    public sealed class XmlReaderMapTaskContext<TOut>
    {
        internal readonly CommandContext CommandContext;
        internal readonly Func<XmlReader, Task<TOut>> MapXmlAsync;

        /// <summary>Initialize with the items needed to preserve the overall command context and support a fluent syntax.</summary>
        internal XmlReaderMapTaskContext(Func<XmlReader, Task<TOut>> mapXmlAsync, CommandContext cmdContext)
        {
            CommandContext = cmdContext;
            MapXmlAsync = mapXmlAsync;
        }

        /// <summary>
        /// Invokes the <paramref name="processDataObject"/> delegate for each instance of <typeparamref name="TOut"/>
        /// mapped while iterating the <see cref="XmlReader"/>.
        /// </summary>
        public XmlActionOverMapTask<TOut> WithDataAction(Action<TOut> processDataObject)
        {
            processDataObject ??= DataAction.DoNothing<TOut>;
            return new XmlActionOverMapTask<TOut>(processDataObject, this);
        }

        /// <summary>
        /// Awaits the <paramref name="processDataObjectAsync"/> task for each instance of <typeparamref name="TOut"/>
        /// mapped while iterating the <see cref="XmlReader"/>.
        /// </summary>
        public XmlTaskOverMapTask<TOut> WithDataTask(Func<TOut, Task> processDataObjectAsync)
        {
            ParamCheck.Assert.IsNotNull(processDataObjectAsync, "processDataObjectAsync");
            return new XmlTaskOverMapTask<TOut>(processDataObjectAsync, this);
        }

        /// <summary>
        /// Returns a task which executes the <see cref="System.Data.Common.DbCommand"/> in context, maps and returns the content of the <see cref="XmlReader"/> as an instance of <typeparamref name="TOut"/>.
        /// </summary>
        public Task<TOut> ExecuteXmlReaderAsync() => CommandContext.ExecuteXmlReaderAsync(MapXmlAsync);

    }
}
