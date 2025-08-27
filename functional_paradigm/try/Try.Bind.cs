namespace contoso.functional;

/// <summary></summary>
public static partial class TryCatch
{
    /// <summary>
    /// Chain two the <c>Result</c> returning functions such that the result value of <paramref name="op"/> is applied to <paramref name="next"/> if not an exception,
    /// otherwise <paramref name="next"/> is not evaluated and just the exception is passed through.
    /// </summary>
    public static Try<TOut> Bind<TIn, TOut>(this Try<TIn> op, Func<TIn, Try<TOut>> next)
        => () => op.Run()
                   .Match
                   (
                       Failure: ex => ex,
                       Success: t => next(t).Run()
                   );
}