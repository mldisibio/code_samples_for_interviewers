using static contoso.functional.FnConstructs;

namespace contoso.functional.advanced;
#pragma warning disable CS1591

public static class TraversableValidation
{
    /// <summary>Traverse an <see cref="Validation{T}"/> into a <see cref="Result{TOut}"/> where TOut is <see cref="Validation{TResult}"/></summary>
    public static Result<Validation<TResult>> Traverse<T, TResult>(this Validation<T> validation, Func<T, Result<TResult>> extract)
        => validation.Match
        (
            Invalid: reasons => Result(Invalid<TResult>(reasons)),
            Valid: t => extract(t).Map(Valid)
        );

    /// <summary>Traverse an <see cref="Validation{T}"/> into a <see cref="Task{TOut}"/> where TOut is <see cref="Validation{TResult}"/></summary>
    public static Task<Validation<TResult>> Traverse<T, TResult>(this Validation<T> validation, Func<T, Task<TResult>> extract)
        => validation.Match
        (
            Invalid: reasons => Async(Invalid<TResult>(reasons)),
            Valid: t => extract(t).Map(Valid)
        );

    internal static Task<Validation<TResult>> TraverseBind<T, TResult>(this Validation<T> validation, Func<T, Task<Validation<TResult>>> extract)
        => validation.Match
        (
            Invalid: reasons => Async(Invalid<TResult>(reasons)),
            Valid: t => extract(t)
        );
}

public static class TraversableResult
{
    /// <summary>Traverse an <see cref="Result{T}"/> into a <see cref="Validation{TOut}"/> where TOut is <see cref="Result{TResult}"/></summary>
    public static Validation<Result<TResult>> Traverse<T, TResult>(this Result<T> result, Func<T, Validation<TResult>> extract)
        => result.Match
        (
              Failure: ex => Valid((Result<TResult>)ex),
              Success: t => from r in extract(t) select Result(r)
        );
}

/*
public static class TraversableTask
{
    public static Validation<Task<TResult>> Traverse<T, TResult>(this Task<T> task, Func<T, Validation<TResult>> func) { throw new NotImplementedException(); }
}
*/
