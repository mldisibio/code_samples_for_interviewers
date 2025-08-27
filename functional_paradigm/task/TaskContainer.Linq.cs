using Unit = System.ValueTuple;

#pragma warning disable CS1591
namespace contoso.functional
{

    public static partial class TaskContainer
    {
        public static async Task<TResult> Select<TSource, TResult>(this Task<TSource> task, Func<TSource, TResult> selector)
            => selector(await task.ConfigureAwait(false));

        public static async Task<TResult> SelectMany<TSource, TCollection, TResult>(this Task<TSource> task, Func<TSource, Task<TCollection>> bind, Func<TSource, TCollection, TResult> project)
        {
            TSource t = await task.ConfigureAwait(false);
            TCollection r = await bind(t).ConfigureAwait(false);
            return project(t, r);
        }

        public static async Task<TResult> SelectMany<TSource, TCollection, TResult>(this Task<TSource> task, Func<TSource, ValueTask<TCollection>> bind, Func<TSource, TCollection, TResult> project)
        {
            TSource t = await task.ConfigureAwait(false);
            TCollection r = await bind(t).ConfigureAwait(false);
            return project(t, r);
        }

        public static async Task<TResult> SelectMany<TCollection, TResult>(this Task task, Func<Unit, Task<TCollection>> bind, Func<Unit, TCollection, TResult> project)
        {
            await task.ConfigureAwait(false);
            TCollection r = await bind(FnConstructs.Unit()).ConfigureAwait(false);
            return project(FnConstructs.Unit(), r);
        }

        public static async Task<R> SelectMany<T, R>(this Task<T> task, Func<T, Task<R>> f)
            => await f(await task.ConfigureAwait(false)).ConfigureAwait(false);
    }
}

namespace contoso.functional.advanced
{
    public static partial class TaskContainerAdvanced
    {
        public static async Task<T> Where<T>(this Task<T> source, Func<T, bool> predicate)
        {
            T t = await source.ConfigureAwait(false);
            if (!predicate(t))
                throw new OperationCanceledException();
            return t;
        }

        public static async Task<V> Join<T, U, K, V>(this Task<T> source,
                                                     Task<U> inner,
                                                     Func<T, K> outerKeySelector,
                                                     Func<U, K> innerKeySelector,
                                                     Func<T, U, V> resultSelector)
        {
            await Task.WhenAll(source, inner).ConfigureAwait(false);
            if (!EqualityComparer<K>.Default.Equals(outerKeySelector(source.Result), innerKeySelector(inner.Result)))
                throw new OperationCanceledException();
            return resultSelector(source.Result, inner.Result);
        }

        public static async Task<V> GroupJoin<T, U, K, V>(this Task<T> source,
                                                          Task<U> inner,
                                                          Func<T, K> outerKeySelector,
                                                          Func<U, K> innerKeySelector,
                                                          Func<T, Task<U>, V> resultSelector)
        {
            T t = await source.ConfigureAwait(false);
            return resultSelector(t, inner.Where(u => EqualityComparer<K>.Default.Equals(outerKeySelector(t), innerKeySelector(u))));
        }
    }
}
#pragma warning restore CS1591