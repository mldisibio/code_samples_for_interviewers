namespace contoso.functional;

public static partial class Either
{
    /// <summary>
    /// Map <typeparamref name="RIn"/> to <typeparamref name="ROut"/> in an elevated context and return a <see cref="Either{L, RResult}"/>
    /// where <typeparamref name="LIn"/> is simply passed through if <paramref name="src"/> is a 'Left'.
    /// </summary>
    public static Either<LIn, ROut> Map<LIn, RIn, ROut>(this Either<LIn, RIn> src, Func<RIn, ROut> mapRight)
            => src.Match<Either<LIn, ROut>>
            (
                Left: l => FnConstructs.Left(l),
                Right: r => FnConstructs.Right(mapRight(r))
            );

    /// <summary>
    /// Map <typeparamref name="RIn"/> to <typeparamref name="ROut"/> or <typeparamref name="LIn"/> to <typeparamref name="LOut"/>
    /// in an elevated context returning an <see cref="Either{LResult, RResult}"/>.
    /// </summary>
    public static Either<LOut, ROut> Map<LIn, LOut, RIn, ROut>(this Either<LIn, RIn> src, Func<RIn, ROut> mapRight, Func<LIn, LOut> mapLeft)
        => src.Match<Either<LOut, ROut>>
        (
            Left: l => FnConstructs.Left(mapLeft(l)),
            Right: r => FnConstructs.Right(mapRight(r))
        );
}