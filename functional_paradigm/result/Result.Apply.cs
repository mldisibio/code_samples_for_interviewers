namespace contoso.functional.advanced;

/// <summary></summary>
public static partial class ResultAdvanced
{
    /// <summary>
    /// If <paramref name="src"/> is a unary function expecting an <typeparamref name="T"/>, and <paramref name="candidate"/> is a valid <typeparamref name="T"/>
    /// apply the function to the argument, otherwise, pass through the Errors of <paramref name="candidate"/>, and/or Errors of <paramref name="src"/>.
    /// </summary>
    public static Result<TResult> Apply<T, TResult>(this Result<Func<T, TResult>> src, Result<T> candidate)
        => src.Match
        (
            Failure: srcEx => candidate.Match
            (
                Failure: candidateEx => new Failure(new AggregateException(srcEx.Exception, candidateEx.Exception), candidateEx.Context, candidateEx.CalledFrom),
                Success: _ => srcEx
            ),
            Success: srcFn => candidate.Match
            (
                Failure: candidateEx => candidateEx,
                Success: candidate => FnConstructs.Result(srcFn(candidate))
            )
        );

    /// <summary>
    /// If <paramref name="src"/> is a function expecting an <typeparamref name="T1"/> as its first argument, and <paramref name="candidate"/> is a success of <typeparamref name="T1"/>
    /// return an elevated function expecting the remaining arguments after the elevated <paramref name="candidate"/> argument has been partially applied.
    /// </summary>
    public static Result<Func<T2, TResult>> Apply<T1, T2, TResult>(this Result<Func<T1, T2, TResult>> src, Result<T1> candidate)
       => Apply(src.Map(FnConstructs.Curry), candidate);

    /// <summary>
    /// If <paramref name="src"/> is a function expecting an <typeparamref name="T1"/> as its first argument, and <paramref name="candidate"/> is a success of <typeparamref name="T1"/>
    /// return an elevated function expecting the remaining arguments after the elevated <paramref name="candidate"/> argument has been partially applied.
    /// </summary>
    public static Result<Func<T2, T3, TResult>> Apply<T1, T2, T3, TResult>(this Result<Func<T1, T2, T3, TResult>> src, Result<T1> candidate)
       => Apply(src.Map(FnConstructs.CurryFirst), candidate);

    /// <summary>
    /// If <paramref name="src"/> is a function expecting an <typeparamref name="T1"/> as its first argument, and <paramref name="candidate"/> is a success of <typeparamref name="T1"/>
    /// return an elevated function expecting the remaining arguments after the elevated <paramref name="candidate"/> argument has been partially applied.
    /// </summary>
    public static Result<Func<T2, T3, T4, TResult>> Apply<T1, T2, T3, T4, TResult>(this Result<Func<T1, T2, T3, T4, TResult>> src, Result<T1> candidate)
       => Apply(src.Map(FnConstructs.CurryFirst), candidate);

    /// <summary>
    /// If <paramref name="src"/> is a function expecting an <typeparamref name="T1"/> as its first argument, and <paramref name="candidate"/> is a success of <typeparamref name="T1"/>
    /// return an elevated function expecting the remaining arguments after the elevated <paramref name="candidate"/> argument has been partially applied.
    /// </summary>
    public static Result<Func<T2, T3, T4, T5, TResult>> Apply<T1, T2, T3, T4, T5, TResult>(this Result<Func<T1, T2, T3, T4, T5, TResult>> src, Result<T1> candidate)
       => Apply(src.Map(FnConstructs.CurryFirst), candidate);

    /// <summary>
    /// If <paramref name="src"/> is a function expecting an <typeparamref name="T1"/> as its first argument, and <paramref name="candidate"/> is a success of <typeparamref name="T1"/>
    /// return an elevated function expecting the remaining arguments after the elevated <paramref name="candidate"/> argument has been partially applied.
    /// </summary>
    public static Result<Func<T2, T3, T4, T5, T6, TResult>> Apply<T1, T2, T3, T4, T5, T6, TResult>(this Result<Func<T1, T2, T3, T4, T5, T6, TResult>> src, Result<T1> candidate)
       => Apply(src.Map(FnConstructs.CurryFirst), candidate);

    /// <summary>
    /// If <paramref name="src"/> is a function expecting an <typeparamref name="T1"/> as its first argument, and <paramref name="candidate"/> is a success of <typeparamref name="T1"/>
    /// return an elevated function expecting the remaining arguments after the elevated <paramref name="candidate"/> argument has been partially applied.
    /// </summary>
    public static Result<Func<T2, T3, T4, T5, T6, T7, TResult>> Apply<T1, T2, T3, T4, T5, T6, T7, TResult>(this Result<Func<T1, T2, T3, T4, T5, T6, T7, TResult>> src, Result<T1> candidate)
       => Apply(src.Map(FnConstructs.CurryFirst), candidate);

    /// <summary>
    /// If <paramref name="src"/> is a function expecting an <typeparamref name="T1"/> as its first argument, and <paramref name="candidate"/> is a success of <typeparamref name="T1"/>
    /// return an elevated function expecting the remaining arguments after the elevated <paramref name="candidate"/> argument has been partially applied.
    /// </summary>
    public static Result<Func<T2, T3, T4, T5, T6, T7, T8, TResult>> Apply<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(this Result<Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult>> src, Result<T1> candidate)
       => Apply(src.Map(FnConstructs.CurryFirst), candidate);

    /// <summary>
    /// If <paramref name="src"/> is a function expecting an <typeparamref name="T1"/> as its first argument, and <paramref name="candidate"/> is a success of <typeparamref name="T1"/>
    /// return an elevated function expecting the remaining arguments after the elevated <paramref name="candidate"/> argument has been partially applied.
    /// </summary>
    public static Result<Func<T2, T3, T4, T5, T6, T7, T8, T9, TResult>> Apply<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(this Result<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>> src, Result<T1> candidate)
       => Apply(src.Map(FnConstructs.CurryFirst), candidate);
}