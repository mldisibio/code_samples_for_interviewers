namespace contoso.functional;

/// <summary></summary>
public static partial class Result
{
    /// <summary>A combination of Map and Select applied to a monad such that LINQ syntax can also be used.</summary>
    public static Result<TResult> Select<TSource, TResult>(this Result<TSource> src, Func<TSource, TResult> selector)
        => src.Map(selector);

    /// <summary>A combination of Bind and Select applied to a monad such that LINQ syntax can also be used.</summary>
    public static Result<TResult> SelectMany<TSource, TCollection, TResult>(this Result<TSource> src, Func<TSource, Result<TCollection>> bind, Func<TSource, TCollection, TResult> project)
        => src.Match
        (
            Failure: ex => Result.Of<TResult>(ex),
            Success: sourceT => bind(sourceT).Match
            (
                Failure: ex => Result.Of<TResult>(ex),
                Success: collectionT => project(sourceT, collectionT)
            )
        );
}