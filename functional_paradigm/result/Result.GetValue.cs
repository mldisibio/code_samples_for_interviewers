namespace contoso.functional;

public static partial class Result
{
    /// <summary>Retrieve the success of <typeparamref name="T"/> or <paramref name="default"/> (eagerly evaluated) if an exception was encountered.</summary>
    public static T GetValueOr<T>(this Result<T> src, T @default)
        => src.Match
        (
            Failure: ex => @default,
            Success: t => t
        );

    /// <summary>Retrieve the success of <typeparamref name="T"/> or lazily evaluate <paramref name="factory"/> if an exception was encountered.</summary>
    public static T GetValueOr<T>(this Result<T> src, Func<T> factory)
        => src.Match
        (
            Failure: ex => factory(),
            Success: t => t
        );
}