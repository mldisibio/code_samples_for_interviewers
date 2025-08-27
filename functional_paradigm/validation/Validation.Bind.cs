namespace contoso.functional;

/// <summary></summary>
public static partial class Validation
{
    /// <summary>
    /// Chain two the <c>Validation</c> returning functions such that the inner value of <paramref name="src"/> is applied to <paramref name="next"/> if <paramref name="src"/> is Valid,
    /// otherwise <paramref name="next"/> is not evaluated and the Errors are passed through.
    /// </summary>
    public static Validation<TOut> Bind<TIn, TOut>(this Validation<TIn> src, Func<TIn, Validation<TOut>> next)
        => src.Match
        (
            Invalid: errs => FnConstructs.Invalid(errs),
            Valid: r => next(r)
        );
}