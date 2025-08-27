namespace contoso.functional;

public static partial class TryCatch
{
    /// <summary>
    /// Apply <paramref name="map"/> to the value returned from <paramref name="op"/> if not an exception. 
    /// Return as <see cref="Try{TOut}"/> which is a delegate that invokes <c>Run</c> but still needs to be invoked itself by calling <c>Match</c> of <see cref="Result{T}"/>.
    /// </summary>
    public static Try<TOut> Map<TIn, TOut>(this Try<TIn> op, Func<TIn, TOut> map)
        => () => op.Run()
                   .Match<Result<TOut>>
                   (
                       Failure: ex => ex,
                       Success: t => map(t)
                   );


    /// <summary>Return an unary function expecting <typeparamref name="T2"/> with the inner value of <paramref name="tryT1"/> partially applied as the first argument of <paramref name="binaryMap"/>.</summary>
    public static Try<Func<T2, TOut>> Map<T1, T2, TOut>(this Try<T1> tryT1, Func<T1, T2, TOut> binaryMap)
        => tryT1.Map(binaryMap.Curry());
}
