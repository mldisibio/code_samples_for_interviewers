namespace contoso.functional;

public static partial class Result
{
    /// <summary>Apply <paramref name="map"/> to the inner value of <paramref name="src"/> if not an exception. Return as <see cref="Result{TOut}"/>.</summary>
    public static Result<TOut> Map<TIn, TOut>(this Result<TIn> src, Func<TIn, TOut> map)
        => src.Match
        (
            Failure: ex => Result.Of<TOut>(ex),
            Success: t => map(t)
        );
}