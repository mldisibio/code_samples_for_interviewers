namespace contoso.functional.advanced;

/// <summary></summary>
public static partial class TaskContainerAdvanced
{
    /// <summary>'Apply' the result from <paramref name="taskOfArg"/> as the first argument to the unary function contained in <paramref name="taskOfFunc"/>.</summary>
    /// <remarks>
    /// When using <c>Apply</c>, each chained function is evaluated. Allows accumulation of errors from multiple operations, for example
    /// </remarks>
    public static async Task<TResult> Apply<T1, TResult>(this Task<Func<T1, TResult>> taskOfFunc, Task<T1> taskOfArg)
        => (await taskOfFunc.ConfigureAwait(false))(await taskOfArg.ConfigureAwait(false));

    /// <summary>Return a task wrapping the curried function expecting the remaining arguments after the result from <paramref name="taskOfArg"/> has been partially applied as the first argument to the function contained in <paramref name="taskOfFunc"/>..</summary>
    public static Task<Func<T2, TResult>> Apply<T1, T2, TResult>(this Task<Func<T1, T2, TResult>> taskOfFunc, Task<T1> taskOfArg)
        => Apply(taskOfFunc.Map(FnConstructs.Curry), taskOfArg);

    /// <summary>Return a task wrapping the curried function expecting the remaining arguments after the result from <paramref name="taskOfArg"/> has been partially applied as the first argument to the function contained in <paramref name="taskOfFunc"/>..</summary>
    public static Task<Func<T2, T3, TResult>> Apply<T1, T2, T3, TResult>(this Task<Func<T1, T2, T3, TResult>> taskOfFunc, Task<T1> taskOfArg)
        => Apply(taskOfFunc.Map(FnConstructs.CurryFirst), taskOfArg);

    /// <summary>Return a task wrapping the curried function expecting the remaining arguments after the result from <paramref name="taskOfArg"/> has been partially applied as the first argument to the function contained in <paramref name="taskOfFunc"/>..</summary>
    public static Task<Func<T2, T3, T4, TResult>> Apply<T1, T2, T3, T4, TResult>(this Task<Func<T1, T2, T3, T4, TResult>> taskOfFunc, Task<T1> taskOfArg)
        => Apply(taskOfFunc.Map(FnConstructs.CurryFirst), taskOfArg);

    /// <summary>Return a task wrapping the curried function expecting the remaining arguments after the result from <paramref name="taskOfArg"/> has been partially applied as the first argument to the function contained in <paramref name="taskOfFunc"/>..</summary>
    public static Task<Func<T2, T3, T4, T5, TResult>> Apply<T1, T2, T3, T4, T5, TResult>(this Task<Func<T1, T2, T3, T4, T5, TResult>> taskOfFunc, Task<T1> taskOfArg)
        => Apply(taskOfFunc.Map(FnConstructs.CurryFirst), taskOfArg);

    /// <summary>Return a task wrapping the curried function expecting the remaining arguments after the result from <paramref name="taskOfArg"/> has been partially applied as the first argument to the function contained in <paramref name="taskOfFunc"/>..</summary>
    public static Task<Func<T2, T3, T4, T5, T6, TResult>> Apply<T1, T2, T3, T4, T5, T6, TResult>(this Task<Func<T1, T2, T3, T4, T5, T6, TResult>> taskOfFunc, Task<T1> taskOfArg)
        => Apply(taskOfFunc.Map(FnConstructs.CurryFirst), taskOfArg);

    /// <summary>Return a task wrapping the curried function expecting the remaining arguments after the result from <paramref name="taskOfArg"/> has been partially applied as the first argument to the function contained in <paramref name="taskOfFunc"/>..</summary>
    public static Task<Func<T2, T3, T4, T5, T6, T7, TResult>> Apply<T1, T2, T3, T4, T5, T6, T7, TResult>(this Task<Func<T1, T2, T3, T4, T5, T6, T7, TResult>> taskOfFunc, Task<T1> taskOfArg)
        => Apply(taskOfFunc.Map(FnConstructs.CurryFirst), taskOfArg);

    /// <summary>Return a task wrapping the curried function expecting the remaining arguments after the result from <paramref name="taskOfArg"/> has been partially applied as the first argument to the function contained in <paramref name="taskOfFunc"/>..</summary>
    public static Task<Func<T2, T3, T4, T5, T6, T7, T8, TResult>> Apply<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(this Task<Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult>> taskOfFunc, Task<T1> taskOfArg)
        => Apply(taskOfFunc.Map(FnConstructs.CurryFirst), taskOfArg);

    /// <summary>Return a task wrapping the curried function expecting the remaining arguments after the result from <paramref name="taskOfArg"/> has been partially applied as the first argument to the function contained in <paramref name="taskOfFunc"/>..</summary>
    public static Task<Func<T2, T3, T4, T5, T6, T7, T8, T9, TResult>> Apply<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(this Task<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>> taskOfFunc, Task<T1> taskOfArg)
        => Apply(taskOfFunc.Map(FnConstructs.CurryFirst), taskOfArg);
}