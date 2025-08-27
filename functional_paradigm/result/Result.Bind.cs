namespace contoso.functional;

public static partial class Result
{
    /// <summary>
    /// Chain two the <c>Result</c> returning functions such that the inner value of <paramref name="src"/> is applied to <paramref name="next"/> if <paramref name="src"/> is Success,
    /// otherwise <paramref name="next"/> is not evaluated and just the exception is passed through.
    /// </summary>
    public static Result<TOut> Bind<TIn, TOut>(this Result<TIn> src, Func<TIn, Result<TOut>> next)
        => src.Match
        (
            Failure: fail => fail, //Result.Of<TOut>(ex),
            Success: r => next(r)
        );
}