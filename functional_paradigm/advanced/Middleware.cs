namespace contoso.functional.advanced;

/// <summary>
/// Defines a function that takes a continuation (a callback) of <typeparamref name="T"/> to <c>R</c>, supplies a <typeparamref name="T"/> to it to obtain the <c>R</c>,
/// and then returns either the same or modified <c>R</c>.
/// </summary>
/// <remarks>
/// This pattern defines a 'Middleware' function which performs setup and teardown, usually while adding some useful data that is passed along the pipeline.
/// Think of starting a stopwatch, performing the core operation, then stopping the stopwatch, and returning the elapsed time along with the operation result.
/// Logging also falls into this pattern, as does opening a closing a db connection.
/// However, while it would seem that <c>R</c> is known at compile time, it is not. 
/// Think of the continuation as 'a function for which we know the input, but that could produce anything' rather than a function that produces a known output.
/// For example, a function that takes a connection string, but could produce any db entity in between opening and closing the connection.
/// Another way is to picture several chained continuations. The input to the first is known, but we cannot tell the compiler what the output of the last will be.
/// However, we can cast the output to a strongly typed <c>R</c> with each specific invocation.
/// </remarks>
public delegate dynamic Middleware<T>(Func<T, dynamic> continuation);

/// <summary></summary>
public static class Middleware
{
    /// <summary>
    /// Run the complete pipeline with the final continuation that just returns <typeparamref name="R"/> such that the dynamic output is both produced and now strongly typed.
    /// </summary>
    public static R Run<R>(this Middleware<R> mw)
        => mw(r => r!);

    /// <summary>
    /// Take the current Middleware <paramref name="mwCurrentOfT"/> that has a <typeparamref name="T"/> to pass into any continuation it will be given
    /// and return a new Middleware that is ready to pass a <typeparamref name="R"/> to any continuation it will be given by telling it how to map <typeparamref name="T"/> to <typeparamref name="R"/>.
    /// </summary>
    public static Middleware<R> Map<T, R>(this Middleware<T> mwCurrentOfT, Func<T, R> makeRFrom)
        => Select(mwCurrentOfT, makeRFrom);

    /// <summary>
    /// Take the current Middleware <paramref name="mwCurrentOfT"/> that has a <typeparamref name="T"/> to pass into any continuation it will be given
    /// and return a new Middleware that is ready to pass a <typeparamref name="R"/> to any continuation it will be given by telling it how to map <typeparamref name="T"/> to <typeparamref name="R"/>.
    /// </summary>
    public static Middleware<R> Select<T, R>(this Middleware<T> mwCurrentOfT, Func<T, R> makeRFrom) //
        => nextContinuationOfR => mwCurrentOfT(t => nextContinuationOfR(makeRFrom(t)));

    /// <summary>
    /// Return a new Middleware delegate that is ready to pipe the <typeparamref name="T"/> output of <paramref name="mwPrevOfT"/> into a <see cref="Middleware{R}"/>.
    /// Supply the <paramref name="makeMwOfR"/> factory method that knows how to create a <see cref="Middleware{R}"/> from the incoming <typeparamref name="T"/>.
    /// </summary>
    public static Middleware<R> Bind<T, R>(this Middleware<T> mwPrevOfT, Func<T, Middleware<R>> makeMwOfR)
        => SelectMany(mwPrevOfT, makeMwOfR);

    /// <summary>
    /// Return a new Middleware delegate that is ready to pipe the <typeparamref name="T"/> output of <paramref name="mwPrevOfT"/> into a <see cref="Middleware{R}"/>.
    /// Supply the <paramref name="makeMwOfR"/> factory method that knows how to create a <see cref="Middleware{R}"/> from the incoming <typeparamref name="T"/>.
    /// </summary>
    public static Middleware<R> SelectMany<T, R>(this Middleware<T> mwPrevOfT, Func<T, Middleware<R>> makeMwOfR)
        => continuationOfR => mwPrevOfT(t => makeMwOfR(t)(continuationOfR));

    /// <summary>
    /// Return a new Middleware delegate that is ready to pipe the <typeparamref name="R"/> output created by combining the <typeparamref name="T1"/> output from <paramref name="mw"/>
    /// with the <typeparamref name="T2"/> output of a second <see cref="Middleware{T2}"/>.
    /// </summary>
    public static Middleware<R> SelectMany<T1, T2, R>(this Middleware<T1> mw, Func<T1, Middleware<T2>> bind, Func<T1, T2, R> project)
        => continuation => mw(t1 => bind(t1)(t2 => continuation(project(t1, t2))));
}

/*
 * See tests for notes on working through what these extensions do, and how to use the extension methods, not just the LINQ query syntax.
 */