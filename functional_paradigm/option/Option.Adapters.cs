using static contoso.functional.FnConstructs;

namespace contoso.functional;

public static partial class Option
{
    /// <summary>Convert <paramref name="src"/> into a <see cref="Validation{T}"/> in the valid state if Some, otherwise invalidated with <paramref name="error"/>.</summary>
    public static Validation<T> ToValidation<T>(this Option<T> src, Error error)
        => src.Match
        (
            None: () => Invalid(error),
            Some: t => Valid(t)
        );

    /// <summary>Convert <paramref name="src"/> into a <see cref="Validation{T}"/> in the valid state if Some, otherwise invalidated by <paramref name="error"/>.</summary>
    public static Validation<T> ToValidation<T>(this Option<T> src, Func<Error> error)
        => src.Match
        (
            None: () => Invalid(error()),
            Some: t => Valid(t)
        );

    /// <summary>
    /// Returns <paramref name="src"/> as a <see cref="Validation{T}"/> in the valid state if Some and <paramref name="validate"/> is true,
    /// otherwise invalidated with <paramref name="error"/>.
    /// </summary>
    public static Validation<T> ValidIf<T>(this Option<T> src, Predicate<T> validate, Error error)
        => src.Match
        (
            None: () => Invalid(error),
            Some: t => validate(t) ? Valid(t) : Invalid(error)
        );
}
