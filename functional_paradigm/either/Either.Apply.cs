namespace contoso.functional.advanced;

/// <summary></summary>
public static partial class EitherAdvanced
{
    /// <summary>
    /// If <paramref name="src"/> is a unary function expecting an <typeparamref name="R"/>, and <paramref name="candidate"/> is an <typeparamref name="R"/>
    /// apply the function to the argument, otherwise, pass through the 'Left' of either <paramref name="src"/> or <paramref name="candidate"/>.
    /// </summary>
    public static Either<L, RResult> Apply<L, R, RResult>(this Either<L, Func<R, RResult>> src, Either<L, R> candidate)
        => src.Match
        (
            Left: noFn => FnConstructs.Left(noFn),
            Right: fn => candidate.Match<Either<L, RResult>>
            (
                Left: candidateLeft => FnConstructs.Left(candidateLeft),
                Right: candidateRight => FnConstructs.Right(fn(candidateRight))
            )
        );

    /// <summary>
    /// If <paramref name="src"/> is a function expecting an <typeparamref name="T1"/> as its first argument, and <paramref name="candidate"/> is an <typeparamref name="T1"/>
    /// return an elevated function expecting the remaining arguments after the elevated <paramref name="candidate"/> argument has been partially applied.
    /// </summary>
    public static Either<L, Func<T2, RResult>> Apply<L, T1, T2, RResult>(this Either<L, Func<T1, T2, RResult>> src, Either<L, T1> candidate)
        => Apply(src.Map(FnConstructs.Curry), candidate);

    /// <summary>
    /// If <paramref name="src"/> is a function expecting an <typeparamref name="T1"/> as its first argument, and <paramref name="candidate"/> is an <typeparamref name="T1"/>
    /// return an elevated function expecting the remaining arguments after the elevated <paramref name="candidate"/> argument has been partially applied.
    /// </summary>
    public static Either<L, Func<T2, T3, RResult>> Apply<L, T1, T2, T3, RResult>(this Either<L, Func<T1, T2, T3, RResult>> src, Either<L, T1> candidate)
        => Apply(src.Map(FnConstructs.CurryFirst), candidate);

    /// <summary>
    /// If <paramref name="src"/> is a function expecting an <typeparamref name="T1"/> as its first argument, and <paramref name="candidate"/> is an <typeparamref name="T1"/>
    /// return an elevated function expecting the remaining arguments after the elevated <paramref name="candidate"/> argument has been partially applied.
    /// </summary>
    public static Either<L, Func<T2, T3, T4, RResult>> Apply<L, T1, T2, T3, T4, RResult>(this Either<L, Func<T1, T2, T3, T4, RResult>> src, Either<L, T1> candidate)
        => Apply(src.Map(FnConstructs.CurryFirst), candidate);

    /// <summary>
    /// If <paramref name="src"/> is a function expecting an <typeparamref name="T1"/> as its first argument, and <paramref name="candidate"/> is an <typeparamref name="T1"/>
    /// return an elevated function expecting the remaining arguments after the elevated <paramref name="candidate"/> argument has been partially applied.
    /// </summary>
    public static Either<L, Func<T2, T3, T4, T5, RResult>> Apply<L, T1, T2, T3, T4, T5, RResult>(this Either<L, Func<T1, T2, T3, T4, T5, RResult>> src, Either<L, T1> candidate)
        => Apply(src.Map(FnConstructs.CurryFirst), candidate);

    /// <summary>
    /// If <paramref name="src"/> is a function expecting an <typeparamref name="T1"/> as its first argument, and <paramref name="candidate"/> is an <typeparamref name="T1"/>
    /// return an elevated function expecting the remaining arguments after the elevated <paramref name="candidate"/> argument has been partially applied.
    /// </summary>
    public static Either<L, Func<T2, T3, T4, T5, T6, RResult>> Apply<L, T1, T2, T3, T4, T5, T6, RResult>(this Either<L, Func<T1, T2, T3, T4, T5, T6, RResult>> src, Either<L, T1> candidate)
        => Apply(src.Map(FnConstructs.CurryFirst), candidate);

    /// <summary>
    /// If <paramref name="src"/> is a function expecting an <typeparamref name="T1"/> as its first argument, and <paramref name="candidate"/> is an <typeparamref name="T1"/>
    /// return an elevated function expecting the remaining arguments after the elevated <paramref name="candidate"/> argument has been partially applied.
    /// </summary>
    public static Either<L, Func<T2, T3, T4, T5, T6, T7, RResult>> Apply<L, T1, T2, T3, T4, T5, T6, T7, RResult>(this Either<L, Func<T1, T2, T3, T4, T5, T6, T7, RResult>> src, Either<L, T1> candidate)
        => Apply(src.Map(FnConstructs.CurryFirst), candidate);

    /// <summary>
    /// If <paramref name="src"/> is a function expecting an <typeparamref name="T1"/> as its first argument, and <paramref name="candidate"/> is an <typeparamref name="T1"/>
    /// return an elevated function expecting the remaining arguments after the elevated <paramref name="candidate"/> argument has been partially applied.
    /// </summary>
    public static Either<L, Func<T2, T3, T4, T5, T6, T7, T8, RResult>> Apply<L, T1, T2, T3, T4, T5, T6, T7, T8, RResult>(this Either<L, Func<T1, T2, T3, T4, T5, T6, T7, T8, RResult>> src, Either<L, T1> candidate)
        => Apply(src.Map(FnConstructs.CurryFirst), candidate);

    /// <summary>
    /// If <paramref name="src"/> is a function expecting an <typeparamref name="T1"/> as its first argument, and <paramref name="candidate"/> is an <typeparamref name="T1"/>
    /// return an elevated function expecting the remaining arguments after the elevated <paramref name="candidate"/> argument has been partially applied.
    /// </summary>
    public static Either<L, Func<T2, T3, T4, T5, T6, T7, T8, T9, RResult>> Apply<L, T1, T2, T3, T4, T5, T6, T7, T8, T9, RResult>(this Either<L, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, RResult>> src, Either<L, T1> candidate)
        => Apply(src.Map(FnConstructs.CurryFirst), candidate);
}