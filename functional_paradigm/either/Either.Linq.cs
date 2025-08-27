namespace contoso.functional;

public static partial class Either
{
    /// <summary>A combination of Map and Select applied to a monad such that LINQ syntax can also be used.</summary>
    public static Either<LSource, TResult> Select<LSource, RSource, TResult>(this Either<LSource, RSource> src, Func<RSource, TResult> selector) => src.Map(selector);

    /// <summary>A combination of Bind and Select applied to a monad such that LINQ syntax can also be used.</summary>
    public static Either<LSource, TResult> SelectMany<LSource, RSource, TCollection, TResult>(this Either<LSource, RSource> src,
                                                                                              Func<RSource, Either<LSource, TCollection>> bind,
                                                                                              Func<RSource, TCollection, TResult> project)
        => src.Match
        (
            Left: sourceL => FnConstructs.Left(sourceL),
            Right: sourceR => bind(sourceR).Match<Either<LSource, TResult>>
            (
                Left: collectionL => FnConstructs.Left(collectionL),
                Right: collectionR => project(sourceR, collectionR)
            )
        );
}