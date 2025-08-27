using System.Collections.Immutable;
using Unit = System.ValueTuple;

namespace contoso.functional;

public static partial class EnumerableContainer
{
    /// <summary>Flatten a collection where each element is an <see cref="IEnumerable{T}"/></summary>
    public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> src)
        => src.SelectMany(x => x);

    /// <summary>
    /// Execute <paramref name="action"/> expected to have a side-effect, on each element of <paramref name="src"/>.
    /// <paramref name="src"/> will be enumerated by this implementation, so execution of <paramref name="action"/> is not deferred.
    /// Returns an <see cref="IEnumerable{Unit}"/> instead of 'void'.
    /// </summary>
    /// <remarks>Keep the scope of <paramref name="action"/> as small as possible and move 'ForEach' as far to the end of chained functions as possible.</remarks>
    public static IEnumerable<Unit> ForEach<T>(this IEnumerable<T> src, Action<T> action)
        => src.Select(action.ToFunc()).ToImmutableList();

    /// <summary>Returns first item in <paramref name="src"/> if not empty, as <see cref="Option{T}"/>. Otherwise returns None.</summary>
    public static Option<T> Head<T>(this IEnumerable<T> src)
    {
        if (src == null)
            return FnConstructs.None;
        var enumerator = src.GetEnumerator();
        return enumerator.MoveNext() ? FnConstructs.Some(enumerator.Current) : FnConstructs.None;
    }

    /// <summary>
    /// Map <paramref name="src"/> to <typeparamref name="TResult"/> 
    /// where <paramref name="Otherwise"/> is a delegate that deconstructs <paramref name="src"/> into a head of <typeparamref name="THead"/>
    /// and tail of <see cref="IEnumerable{THead}"/> which can be empty, and does not include the head.
    /// </summary>
    public static TResult Match<THead, TResult>(this IEnumerable<THead> src, Func<TResult> Empty, Func<THead, IEnumerable<THead>, TResult> Otherwise)
        => src.Head()
              .Match
        (
            None: Empty,
            Some: head => Otherwise(head, src.Skip(1))
        );

    /// <summary>Return first item in <paramref name="src"/> matching <paramref name="predicate"/> as <see cref="Option{T}"/> otherwise None.</summary>
    public static Option<T> Find<T>(this IEnumerable<T> src, Func<T, bool> predicate)
       => src.Where(predicate).Head();

    /// <summary>Deferred, non-destructive append to end of <paramref name="src"/>.</summary>
    public static IEnumerable<T> Append<T>(this IEnumerable<T> src, params T[] items)
        => src.Concat(items);

    /// <summary>Deferred, non-destructive insert at beginning of <paramref name="src"/>.</summary>
    public static IEnumerable<T> Prepend<T>(this IEnumerable<T> src, T item)
    {
        yield return item;
        foreach (T t in src)
            yield return t;
    }

    /// <summary></summary>
    public static (IEnumerable<T> Passed, IEnumerable<T> Failed) Partition<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        var partitions = source.GroupBy(predicate); // group.Key is <bool>
        return
        (
           Passed: partitions.Where(grp => grp.Key).FirstOrDefault(Enumerable.Empty<T>()),
           Failed: partitions.Where(grp => !grp.Key).FirstOrDefault(Enumerable.Empty<T>())
        );
    }
}
