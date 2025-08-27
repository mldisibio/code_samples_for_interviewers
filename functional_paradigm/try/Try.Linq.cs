namespace contoso.functional;

public static partial class TryCatch
{
    /// <summary>A combination of Map and Select applied to a monad such that LINQ syntax can also be used.</summary>
    public static Try<TResult> Select<TSource, TResult>(this Try<TSource> op, Func<TSource, TResult> selector)
        => op.Map(selector);

    /// <summary>A combination of Bind and Select applied to a monad such that LINQ syntax can also be used.</summary>
    public static Try<TResult> SelectMany<TSource, TCollection, TResult>(this Try<TSource> op, Func<TSource, Try<TCollection>> bind, Func<TSource, TCollection, TResult> project)
        => () => op.Run()
                   .Match
              (
                  Failure: ex => ex,
                  Success: sourceT => bind(sourceT).Run()
                                                   .Match<Result<TResult>>
                                   (
                                       Failure: ex => ex,
                                       Success: collectionT => project(sourceT, collectionT)
                                   )
              );
}
