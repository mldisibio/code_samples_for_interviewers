namespace contoso.functional;

public static partial class Validation
{
    /// <summary>A combination of Map and Select applied to a monad such that LINQ syntax can also be used.</summary>
    public static Validation<TResult> Select<TSource, TResult>(this Validation<TSource> src, Func<TSource, TResult> selector)
        => src.Map(selector);

    /// <summary>A combination of Bind and Select applied to a monad such that LINQ syntax can also be used.</summary>
    public static Validation<TResult> SelectMany<TSource, TCollection, TResult>(this Validation<TSource> src, Func<TSource, Validation<TCollection>> bind, Func<TSource, TCollection, TResult> project)
        => src.Match
        (
            Invalid: errs => FnConstructs.Invalid(errs),
            Valid: sourceT => bind(sourceT).Match
            (
                Invalid: errs => FnConstructs.Invalid(errs),
                Valid: collectionT => FnConstructs.Valid(project(sourceT, collectionT))
            )
        );
}