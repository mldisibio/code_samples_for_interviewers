namespace contoso.functional;

public static partial class Validation
{
    /// <summary>
    /// Map the <typeparamref name="TIn"/> value of <paramref name="src"/> to <typeparamref name="TOut"/> wrapped as <see cref="Validation{TOut}"/>
    /// If <paramref name="src"/> is Invalid, Errors are simply passed through.
    /// </summary>
    public static Validation<TOut> Map<TIn, TOut>(this Validation<TIn> src, Func<TIn, TOut> map)
        => src.Match
        (
            Invalid: errs => FnConstructs.Invalid(errs),
            Valid: t => FnConstructs.Valid(map(t))
        );

    /// <summary>Return an unary function expecting <typeparamref name="T2"/> with the inner value of <paramref name="validT1"/> partially applied as the first argument of <paramref name="binaryMap"/>.</summary>
    public static Validation<Func<T2, TOut>> Map<T1, T2, TOut>(this Validation<T1> validT1, Func<T1, T2, TOut> binaryMap)
        => validT1.Map(binaryMap.Curry());
}