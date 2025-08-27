using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using contoso.ado.Common;
using contoso.ado.DataActions;
using contoso.ado.Internals;

namespace contoso.ado.Fluent
{
    /// <summary>
    /// Simple fluent artifact to guide command execution path when a task for mapping an
    /// <see cref="DbDataReader"/> record is specified as part of the caller's command context.
    /// </summary>
    public sealed class ReaderMapTaskContext<TOut>
    {
        internal readonly CommandContext CommandContext;
        internal readonly Func<DbDataReader, Task<TOut?>> MapRowAsync;

        /// <summary>Initialize with the items needed to preserve the overall command context and support a fluent syntax.</summary>
        internal ReaderMapTaskContext(Func<DbDataReader, Task<TOut?>> mapRowAsync, CommandContext cmdContext)
        {
            CommandContext = cmdContext;
            MapRowAsync = mapRowAsync;
        }

        /// <summary>
        /// Invokes the <paramref name="processDataObject"/> delegate for each instance of <typeparamref name="TOut"/>
        /// mapped while iterating the <see cref="DbDataReader"/>.
        /// </summary>
        public DataActionOverMapTask<TOut?> WithDataAction(Action<TOut?> processDataObject)
        {
            processDataObject ??= DataAction.DoNothing<TOut?>;
            return new DataActionOverMapTask<TOut?>(processDataObject, this);
        }

        /// <summary>
        /// Awaits the <paramref name="processDataObjectAsync"/> task for each instance of <typeparamref name="TOut"/>
        /// mapped while iterating the <see cref="DbDataReader"/>.
        /// </summary>
        public DataTaskOverMapTask<TOut?> WithDataTask(Func<TOut?, Task> processDataObjectAsync)
        {
            ParamCheck.Assert.IsNotNull(processDataObjectAsync, "processDataObjectAsync");
            return new DataTaskOverMapTask<TOut?>(processDataObjectAsync, this);
        }

        /// <summary>
        /// Returns a task which executes the <see cref="DbCommand"/> in context, iterates the <see cref="DbDataReader"/> 
        /// and returns the rows as a List of <typeparamref name="TOut"/>.
        /// </summary>
        public async Task<List<TOut?>> ExecuteReaderAsync()
        {
            var wrapper = ListDataAction<TOut?>.Default();
            await CommandContext.ExecuteReaderAsync(MapRowAsync, wrapper.AddToList).ConfigureAwait(false);
            return wrapper.CopyAndClear();
        }

        /// <summary>
        /// Returns a task which executes the <see cref="DbCommand"/> in context, maps and returns the first row of <see cref="DbDataReader"/> as an instance of <typeparamref name="TOut"/>.
        /// </summary>
        public Task<TOut?> ExecuteScalarReaderAsync() => CommandContext.ExecuteScalarReaderAsync(MapRowAsync);

    }
}
