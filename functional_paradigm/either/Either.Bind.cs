namespace contoso.functional;

/// <summary></summary>
public static partial class Either
{
    /// <summary>
    /// Apply <paramref name="fn"/> if <paramref name="src"/> is an <typeparamref name="R"/>.
    /// Since <paramref name="fn"/> already produces an  <see cref="Either{L, RResult}"/>, return the 'flattened' outcome.
    /// </summary>
    public static Either<L, RResult> Bind<L, R, RResult>(this Either<L, R> src, Func<R, Either<L, RResult>> fn)
        => src.Match
        (
            Left: l => FnConstructs.Left(l),
            Right: r => fn(r)
        );
}