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
    /// Simple fluent artifact to guide command execution path when a delegate for mapping an
    /// <see cref="DbDataReader"/> record is specified as part of the caller's command context.
    /// </summary>
    public sealed class ReaderMapContext<TOut>
    {
        internal readonly CommandContext CommandContext;
        internal readonly Func<DbDataReader, TOut?> MapRow;

        /// <summary>Initialize with the items needed to preserve the overall command context and support a fluent syntax.</summary>
        internal ReaderMapContext(Func<DbDataReader, TOut?> mapRow, CommandContext cmdContext)
        {
            CommandContext = cmdContext;
            MapRow = mapRow;
        }

        /// <summary>
        /// Invokes the <paramref name="processDataObject"/> delegate for each instance of <typeparamref name="TOut"/>
        /// mapped while iterating the <see cref="DbDataReader"/>.
        /// </summary>
        public DataActionOverMapping<TOut?> WithDataAction(Action<TOut?> processDataObject)
        {
            processDataObject ??= DataAction.DoNothing<TOut?>;
            return new DataActionOverMapping<TOut?>(processDataObject, this);
        }

        /// <summary>
        /// Awaits the <paramref name="processDataObjectAsync"/> task for each instance of <typeparamref name="TOut"/>
        /// mapped while iterating the <see cref="DbDataReader"/>.
        /// </summary>
        public DataTaskOverMapping<TOut?> WithDataTask(Func<TOut?, Task> processDataObjectAsync)
        {
            ParamCheck.Assert.IsNotNull(processDataObjectAsync, "processDataObjectAsync");
            return new DataTaskOverMapping<TOut?>(processDataObjectAsync, this);
        }

        /// <summary>
        /// Executes the <see cref="DbCommand"/> in context, iterates the <see cref="DbDataReader"/>
        /// and returns the collection of rows mapped to <typeparamref name="TOut"/> as a list.
        /// </summary>
        public List<TOut?> ExecuteReader()
        {
            var wrapper = ListDataAction<TOut?>.Default();
            CommandContext.ExecuteReader(MapRow, wrapper.AddToList);
            return wrapper.CopyAndClear();
        }

        /// <summary>
        /// Returns a task which executes the <see cref="DbCommand"/> in context, iterates the <see cref="DbDataReader"/>
        /// and returns the collection of rows mapped to <typeparamref name="TOut"/> as a list.
        /// </summary>
        public async Task<List<TOut?>> ExecuteReaderAsync()
        {
            var wrapper = ListDataAction<TOut?>.Default();
            await CommandContext.ExecuteReaderAsync(MapRow, wrapper.AddToList).ConfigureAwait(false);
            return wrapper.CopyAndClear();
        }

        /// <summary>
        /// Executes the <see cref="DbCommand"/> in context, maps and returns the first row of <see cref="DbDataReader"/> as an instance of <typeparamref name="TOut"/>.
        /// </summary>
        public TOut? ExecuteScalarReader() => CommandContext.ExecuteScalarReader(MapRow);

        /// <summary>
        /// Returns a task which executes the <see cref="DbCommand"/> in context, maps and returns the first row of <see cref="DbDataReader"/> as an instance of <typeparamref name="TOut"/>.
        /// </summary>
        public Task<TOut?> ExecuteScalarReaderAsync() => CommandContext.ExecuteScalarReaderAsync(MapRow);

    }
}
