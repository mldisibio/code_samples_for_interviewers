using static contoso.functional.FnConstructs;

namespace contoso.functional.advanced;

#pragma warning disable CS1591
public static class TraversableEnumerable
{
    /*
    static IEnumerable<T> ConstructFromHeadAndTail<T>(this T head, IEnumerable<T> tail) => ListOf(head).Concat(tail);

    static Func<T, IEnumerable<T>, IEnumerable<T>> ListConstructor<T>() => (head, tail) => head.ConstructFromHeadAndTail(tail);
    */
    static Func<IEnumerable<T>, T, IEnumerable<T>> Append<T>() => (tail, last) => tail.Append(last);

    #region Result

    /// <summary>
    /// Aggregate a collection of <see cref="Result{T}"/> into a single <c>Result</c> of <see cref="IEnumerable{TResult}"/> 
    /// such that the output is Failed if any one of the inputs is Failed, otherwise the output is Success and its Value is an <see cref="IEnumerable{TResult}"/> where each item is Success.
    /// </summary>
    public static Result<IEnumerable<TResult>> Traverse<T, TResult>(this IEnumerable<T> src, Func<T, Result<TResult>> extract)
        => src.Aggregate
        (
            seed: Result.Of(Enumerable.Empty<TResult>()),
            // Exceptional<[R]> -> T -> Exceptional<[R]>
            func: (accumulator, t) => from results in accumulator
                                      from nxt in extract(t)
                                      select results.Append(nxt)
        );

    #endregion

    #region Option

    /// <summary>
    /// Aggregate a collection of <see cref="Option{T}"/> into a single <c>Option</c> of <see cref="IEnumerable{TResult}"/> 
    /// such that the output is None if any one of the inputs is None, otherwise the output is Some and its Value is an <see cref="IEnumerable{TResult}"/> where each item is a Some.
    /// </summary>
    public static Option<IEnumerable<TResult>> Traverse<T, TResult>(this IEnumerable<T> src, Func<T, Option<TResult>> extract)
        => src.Aggregate
        (
            seed: Some(Enumerable.Empty<TResult>()),
            // Option<[R]> -> T -> Option<[R]>
            func: (accumulator, t) => from results in accumulator
                                      from nxt in extract(t)
                                      select results.Append(nxt)
        );

    /// <summary>Applicative Traverse</summary>
    internal static Option<IEnumerable<TResult>> TraverseA<T, TResult>(this IEnumerable<T> src, Func<T, Option<TResult>> extract)
        => src.Aggregate
        (
            seed: Some(Enumerable.Empty<TResult>()),
            func: (accumulator, t) => Some(Append<TResult>()).Apply(accumulator).Apply(extract(t))
        );

    /// <summary>Monadic Traverse</summary>
    internal static Option<IEnumerable<TResult>> TraverseM<T, TResult>(this IEnumerable<T> src, Func<T, Option<TResult>> extract) => Traverse(src, extract);

    #endregion

    #region Validation

    /// <summary>
    /// Aggregate a collection of <see cref="Validation{T}"/> into a single <c>Validation</c> of <see cref="IEnumerable{TResult}"/> 
    /// such that the output is Invalid if any one of the inputs is Invalid, otherwise the output is Valid and its Value is an <see cref="IEnumerable{TResult}"/> where each item is Valid.
    /// </summary>
    public static Validation<IEnumerable<TResult>> Traverse<T, TResult>(this IEnumerable<T> src, Func<T, Validation<TResult>> extract) => TraverseA(src, extract);

    /// <summary>Applicative Traverse</summary>
    internal static Validation<IEnumerable<TResult>> TraverseA<T, TResult>(this IEnumerable<T> src, Func<T, Validation<TResult>> extract)
        => src.Aggregate
        (
            seed: Valid(Enumerable.Empty<TResult>()),
            func: (accumulator, t) => Valid(Append<TResult>()).Apply(accumulator).Apply(extract(t)));

    /// <summary>Monadic Traverse</summary>
    internal static Validation<IEnumerable<TResult>> TraverseM<T, TResult>(this IEnumerable<T> src, Func<T, Validation<TResult>> extract)
        => src.Aggregate
        (
            seed: Valid(Enumerable.Empty<TResult>()),
            // Validation<[R]> -> T -> Validation<[R]>
            func: (accumulator, t) => from results in accumulator
                                      from nxt in extract(t)
                                      select results.Append(nxt)
        );

    #endregion

    #region Task

    /// <summary>
    /// Aggregate a collection of <see cref="Task{T}"/> into a single <c>Task</c> of <see cref="IEnumerable{TResult}"/> 
    /// such that the output is Faulted if any one of the inputs is Faulted, otherwise the output is Completed and its Value is an <see cref="IEnumerable{TResult}"/> where each item is a Completed task.
    /// </summary>
    public static Task<IEnumerable<TResult>> Traverse<T, TResult>(this IEnumerable<T> src, Func<T, Task<TResult>> extract)
        // by default use applicative traverse (parallel, hence faster)
        => TraverseA(src, extract);

    /// <summary>Applicative Traverse</summary>
    internal static Task<IEnumerable<TResult>> TraverseA<T, TResult>(this IEnumerable<T> src, Func<T, Task<TResult>> extract)
        => src.Aggregate
        (
            seed: Task.FromResult(Enumerable.Empty<TResult>()),
            func: (accumulator, t) => Task.FromResult(Append<TResult>()).Apply(accumulator).Apply(extract(t)));

    /// <summary>Monadic Traverse</summary>
    internal static Task<IEnumerable<TResult>> TraverseM<T, TResult>(this IEnumerable<T> src, Func<T, Task<TResult>> extract)
        => src.Aggregate
        (
            seed: Task.FromResult(Enumerable.Empty<TResult>()),
            // Task<[R]> -> T -> Task<[R]>
            func: (accumulator, t) => from results in accumulator
                                      from nxt in extract(t)
                                      select results.Append(nxt)
        );

    #endregion
}
#pragma warning restore CS1591