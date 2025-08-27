namespace contoso.functional;

/// <summary></summary>
public static partial class TaskContainer
{
    /// <summary>Apply the Result of <paramref name="task"/> to <paramref name="next"/>, executed sequentially independent of the completion status of <paramref name="task"/>.</summary>
    public static async Task<TResult> Bind<TSource, TResult>(this Task<TSource> task, Func<TSource, Task<TResult>> next)
        => await next(await task.ConfigureAwait(false)).ConfigureAwait(false);
}