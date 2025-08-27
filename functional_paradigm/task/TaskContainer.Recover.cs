namespace contoso.functional;

public static partial class TaskContainer
{
    /// <summary>
    /// Execute <paramref name="fallback"/> if <paramref name="task"/> completed in the Faulted state,
    /// handling the exception and returning a fallback <typeparamref name="T"/>.
    /// otherwise return Result <typeparamref name="T"/> from <paramref name="task"/>.
    /// </summary>
    public static Task<T> Recover<T>(this Task<T> task, Func<Exception, T> fallback)
        => task.ContinueWith
        (
            prev => prev.Status == TaskStatus.Faulted
                    ? fallback(prev.Exception!)
                      : prev.Status == TaskStatus.Canceled
                        ? fallback(new TaskCanceledException())
                        : prev.Result
        );

    /// <summary>
    /// Await <paramref name="fallbackTask"/> if <paramref name="task"/> completed in the Faulted state,
    /// handling the exception and returning a fallback <typeparamref name="T"/>.
    /// otherwise return Result <typeparamref name="T"/> from <paramref name="task"/>.
    /// </summary>
    public static Task<T> RecoverAsync<T>(this Task<T> task, Func<Exception, Task<T>> fallbackTask)
        => task.ContinueWith
        (
            prev => prev.Status == TaskStatus.Faulted
                    ? fallbackTask(prev.Exception!)
                      : prev.Status == TaskStatus.Canceled
                        ? fallbackTask(new TaskCanceledException())
                        : Task.FromResult(prev.Result)
        ).Unwrap();
}