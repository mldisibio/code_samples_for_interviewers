namespace contoso.functional.advanced;

/// <summary></summary>
public static partial class ValidationAdvanced
{
    /// <summary>
    /// If <paramref name="src"/> is a unary function expecting an <typeparamref name="T"/>, and <paramref name="candidate"/> is a valid <typeparamref name="T"/>
    /// apply the function to the argument, otherwise, pass through the Errors of <paramref name="candidate"/>, and/or Errors of <paramref name="src"/>.
    /// </summary>
    public static Validation<TResult> Apply<T, TResult>(this Validation<Func<T, TResult>> src, Validation<T> candidate)
        => src.Match
        (
            Invalid: srcErrs => candidate.Match
            (
                Invalid: candidateErrs => FnConstructs.Invalid(srcErrs.Concat(candidateErrs)),
                Valid: _ => FnConstructs.Invalid(srcErrs)
            ),
            Valid: srcFn => candidate.Match
            (
                Invalid: candidateErrs => FnConstructs.Invalid(candidateErrs),
                Valid: candidate => FnConstructs.Valid(srcFn(candidate))
            )
        );

    /// <summary>
    /// If <paramref name="src"/> is a function expecting an <typeparamref name="T1"/> as its first argument, and <paramref name="candidate"/> is a valid <typeparamref name="T1"/>
    /// return an elevated function expecting the remaining arguments after the elevated <paramref name="candidate"/> argument has been partially applied.
    /// </summary>
    public static Validation<Func<T2, TResult>> Apply<T1, T2, TResult>(this Validation<Func<T1, T2, TResult>> src, Validation<T1> candidate)
       => Apply(src.Map(FnConstructs.Curry), candidate);

    /// <summary>
    /// If <paramref name="src"/> is a function expecting an <typeparamref name="T1"/> as its first argument, and <paramref name="candidate"/> is a valid <typeparamref name="T1"/>
    /// return an elevated function expecting the remaining arguments after the elevated <paramref name="candidate"/> argument has been partially applied.
    /// </summary>
    public static Validation<Func<T2, T3, TResult>> Apply<T1, T2, T3, TResult>(this Validation<Func<T1, T2, T3, TResult>> src, Validation<T1> candidate)
       => Apply(src.Map(FnConstructs.CurryFirst), candidate);

    /// <summary>
    /// If <paramref name="src"/> is a function expecting an <typeparamref name="T1"/> as its first argument, and <paramref name="candidate"/> is a valid <typeparamref name="T1"/>
    /// return an elevated function expecting the remaining arguments after the elevated <paramref name="candidate"/> argument has been partially applied.
    /// </summary>
    public static Validation<Func<T2, T3, T4, TResult>> Apply<T1, T2, T3, T4, TResult>(this Validation<Func<T1, T2, T3, T4, TResult>> src, Validation<T1> candidate)
       => Apply(src.Map(FnConstructs.CurryFirst), candidate);

    /// <summary>
    /// If <paramref name="src"/> is a function expecting an <typeparamref name="T1"/> as its first argument, and <paramref name="candidate"/> is a valid <typeparamref name="T1"/>
    /// return an elevated function expecting the remaining arguments after the elevated <paramref name="candidate"/> argument has been partially applied.
    /// </summary>
    public static Validation<Func<T2, T3, T4, T5, TResult>> Apply<T1, T2, T3, T4, T5, TResult>(this Validation<Func<T1, T2, T3, T4, T5, TResult>> src, Validation<T1> candidate)
       => Apply(src.Map(FnConstructs.CurryFirst), candidate);

    /// <summary>
    /// If <paramref name="src"/> is a function expecting an <typeparamref name="T1"/> as its first argument, and <paramref name="candidate"/> is a valid <typeparamref name="T1"/>
    /// return an elevated function expecting the remaining arguments after the elevated <paramref name="candidate"/> argument has been partially applied.
    /// </summary>
    public static Validation<Func<T2, T3, T4, T5, T6, TResult>> Apply<T1, T2, T3, T4, T5, T6, TResult>(this Validation<Func<T1, T2, T3, T4, T5, T6, TResult>> src, Validation<T1> candidate)
       => Apply(src.Map(FnConstructs.CurryFirst), candidate);

    /// <summary>
    /// If <paramref name="src"/> is a function expecting an <typeparamref name="T1"/> as its first argument, and <paramref name="candidate"/> is a valid <typeparamref name="T1"/>
    /// return an elevated function expecting the remaining arguments after the elevated <paramref name="candidate"/> argument has been partially applied.
    /// </summary>
    public static Validation<Func<T2, T3, T4, T5, T6, T7, TResult>> Apply<T1, T2, T3, T4, T5, T6, T7, TResult>(this Validation<Func<T1, T2, T3, T4, T5, T6, T7, TResult>> src, Validation<T1> candidate)
       => Apply(src.Map(FnConstructs.CurryFirst), candidate);

    /// <summary>
    /// If <paramref name="src"/> is a function expecting an <typeparamref name="T1"/> as its first argument, and <paramref name="candidate"/> is a valid <typeparamref name="T1"/>
    /// return an elevated function expecting the remaining arguments after the elevated <paramref name="candidate"/> argument has been partially applied.
    /// </summary>
    public static Validation<Func<T2, T3, T4, T5, T6, T7, T8, TResult>> Apply<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(this Validation<Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult>> src, Validation<T1> candidate)
       => Apply(src.Map(FnConstructs.CurryFirst), candidate);

    /// <summary>
    /// If <paramref name="src"/> is a function expecting an <typeparamref name="T1"/> as its first argument, and <paramref name="candidate"/> is a valid <typeparamref name="T1"/>
    /// return an elevated function expecting the remaining arguments after the elevated <paramref name="candidate"/> argument has been partially applied.
    /// </summary>
    public static Validation<Func<T2, T3, T4, T5, T6, T7, T8, T9, TResult>> Apply<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(this Validation<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>> src, Validation<T1> candidate)
       => Apply(src.Map(FnConstructs.CurryFirst), candidate);
}