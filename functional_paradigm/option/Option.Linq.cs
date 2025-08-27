using static contoso.functional.FnConstructs;

namespace contoso.functional;

public static partial class Option
{
    /// <summary>A combination of Map and Select applied to a monad such that LINQ syntax can also be used.</summary>
    public static Option<TResult> Select<TSource, TResult>(this Option<TSource> src, Func<TSource, TResult> selector)
        => src.Map(selector);

    /// <summary>A LINQ compatible where clause returning <paramref name="src"/> if <paramref name="predicate"/> is matched, otherwise None.</summary>
    public static Option<T> Where<T>(this Option<T> src, Func<T, bool> predicate)
        => src.Match
        (
            None: () => None,
            Some: t => predicate(t) ? src : None
        );

    /// <summary>A combination of Bind and Select applied to a monad such that LINQ syntax can also be used.</summary>
    public static Option<TResult> SelectMany<TSource, TCollection, TResult>(this Option<TSource> src, Func<TSource, Option<TCollection>> bind, Func<TSource, TCollection, TResult> project)
        => src.Match
        (
            None: () => None,
            Some: sourceT => bind(sourceT).Match
            (
                None: () => None,
                Some: collectionT => Some(project(sourceT, collectionT))
            )
        );
}