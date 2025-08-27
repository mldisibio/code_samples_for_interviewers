namespace contoso.functional;

public static partial class Validation
{
    /// <summary>Retrieve the validated <typeparamref name="T"/> or <paramref name="default"/> (eagerly evaluated) if invalid.</summary>
    public static T GetValueOr<T>(this Validation<T> src, T @default)
        => src.Match
        (
            Invalid: errs => @default,
            Valid: t => t
        );

    /// <summary>Retrieve the validated <typeparamref name="T"/> or lazily evaluate <paramref name="factory"/> if invalid.</summary>
    public static T GetValueOr<T>(this Validation<T> src, Func<T> factory)
        => src.Match
        (
            Invalid: errs => factory(),
            Valid: t => t
        );
}