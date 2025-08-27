using static contoso.functional.FnConstructs;

namespace contoso.functional.advanced;
#pragma warning disable CS1591


public static class OptionTraversable
{
    /// <summary>Traverse an <see cref="Option{T}"/> into a <see cref="Result{TOut}"/> where TOut is <see cref="Option{TResult}"/></summary>
    public static Result<Option<TResult>> Traverse<T, TResult>(this Option<T> option, Func<T, Result<TResult>> extract)
        => option.Match
        (
            None: () => Result((Option<TResult>)None),
            Some: t => extract(t).Map(Some)
        );

    /// <summary>Traverse an <see cref="Option{T}"/> into a <see cref="Task{TOut}"/> where TOut is <see cref="Option{TResult}"/></summary>
    public static Task<Option<TResult>> Traverse<T, TResult>(this Option<T> option, Func<T, Task<TResult>> extract)
        => option.Match
        (
            None: () => Async((Option<TResult>)None),
            Some: t => extract(t).Map(Some)
        );

    internal static Task<Option<TResult>> TraverseBind<T, TResult>(this Option<T> option, Func<T, Task<Option<TResult>>> extract)
        => option.Match
        (
            None: () => Async((Option<TResult>)None),
            Some: t => extract(t)
        );
}
