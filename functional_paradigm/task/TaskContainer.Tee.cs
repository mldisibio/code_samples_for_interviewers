using Unit = System.ValueTuple;

namespace contoso.functional;

public static partial class TaskContainer
{
    /// <summary>
    /// Execute <paramref name="Completed"/> (action with side-effects) upon completion of <paramref name="task"/> or or apply <paramref name="Faulted"/> to it's exception.
    /// Returns an implicit <see cref="Task{Unit}"/> meaning it should be last task in any functional chain.
    /// </summary>
    /// <remarks>Will apply <paramref name="Faulted"/> to <see cref="TaskCanceledException"/> if <paramref name="task"/> was cancelled and has an empty exception.</remarks>
    public static Task<Unit> Do(this Task task, Action<Exception> Faulted, Action? Completed = null)
        => task.ContinueWith
        (
            prev => prev.Status == TaskStatus.Faulted
                    ? Faulted.ToFunc()(prev.Exception!)
                    : prev.Status == TaskStatus.Canceled
                      ? Faulted.ToFunc()(new TaskCanceledException())
                      : (Completed ?? _doNothing).ToFunc()()
        );

    /// <summary>
    /// Execute <paramref name="Completed"/> (action with side-effects) using the <typeparamref name="T"/> Result of <paramref name="task"/>,
    /// or or apply <paramref name="Faulted"/> to it's exception.
    /// Returns an implicit <see cref="Task{Unit}"/> meaning it should be last task in any functional chain.
    /// </summary>
    /// <remarks>Will apply <paramref name="Faulted"/> to <see cref="TaskCanceledException"/> if <paramref name="task"/> was cancelled and has an empty exception.</remarks>
    public static Task<Unit> Do<T>(this Task<T> task, Action<Exception> Faulted, Action<T> Completed)
        => task.ContinueWith
        (
            prev => prev.Status == TaskStatus.Faulted
                    ? Faulted.ToFunc()(prev.Exception!)
                    : prev.Status == TaskStatus.Canceled
                      ? Faulted.ToFunc()(new TaskCanceledException())
                      : Completed.ToFunc()(prev.Result)
        );

    /// <summary>
    /// Execute <paramref name="Completed"/> (task with side-effects) using the <typeparamref name="T"/> Result of <paramref name="task"/>,
    /// or or apply <paramref name="Faulted"/> to it's exception.
    /// Returns an implicit <see cref="Task{Unit}"/> meaning it should be last task in any functional chain.
    /// </summary>
    /// <remarks>Will apply <paramref name="Faulted"/> to <see cref="TaskCanceledException"/> if <paramref name="task"/> was cancelled and has an empty exception.</remarks>
    public static Task<Unit> DoAsync<T>(this Task<T> task, Func<Exception, Task> Faulted, Func<T, Task> Completed)
        => task.ContinueWith
        (
            prev =>
            {
                if (prev.Status == TaskStatus.Faulted)
                    Faulted(prev.Exception!);
                else if (prev.Status == TaskStatus.Canceled)
                    Faulted(new TaskCanceledException());
                else
                    Completed(prev.Result);
                return FnConstructs.Unit();
            }
        );

    /// <summary>
    /// Execute <paramref name="action"/> (action with side-effects) only if <paramref name="src"/> completed without faulting or cancellation.
    /// Returns an implicit <see cref="Task{Unit}"/> meaning it should be last task in any functional chain.
    /// However, if <paramref name="src"/> completed in the Faulted state, the continuation will now represent a new task in the Canceled state, with no exception.
    /// </summary>
    /// <remarks>While <c>IsCompleted</c> is true for <c>RanToCompletion, Faulted, Cancelled</c>, RanToCompletion is the status only if completed without faulting or cancellation.</remarks>
    public static Task<Unit> ForEach<T>(this Task<T> src, Action<T> action)
        => src.ContinueWith(t => action.ToFunc()(t.Result), TaskContinuationOptions.OnlyOnRanToCompletion);


    /// <summary>
    /// Execute <paramref name="action"/> (action with side-effects) only if <paramref name="src"/> completed without faulting or cancellation.
    /// Returns an implicit <see cref="Task{Unit}"/> meaning it should be last task in any functional chain.
    /// However, if <paramref name="src"/> completed in the Faulted state, the continuation will now represent a new task in the Canceled state, with no exception.
    /// </summary>
    /// <remarks>Fluent pass-thru to <see cref="ForEach{T}(Task{T}, Action{T})"/>.</remarks>
    public static Task<Unit> OnCompletionOnlyDo<T>(this Task<T> src, Action<T> action) => src.ForEach(action);

    /// <summary>
    /// Execute <paramref name="action"/> (action with side-effects) while passing <paramref name="src"/> through, but only if <paramref name="src"/> completed without faulting or cancellation.
    /// </summary>
    public static Task<T> TeeOnCompletion<T>(this Task<T> src, Action<T> action)
        => src.ContinueWith(prev =>
        {
            if (prev.Status == TaskStatus.RanToCompletion)
                action(prev.Result);
            return prev;
        })
        .Unwrap();

    /// <summary>
    /// Execute <paramref name="task"/> (action with side-effects) while passing <paramref name="src"/> through, but only if <paramref name="src"/> completed without faulting or cancellation.
    /// </summary>
    public static Task<T> TeeAsyncOnCompletion<T>(this Task<T> src, Func<T, Task> task)
        => src.ContinueWith(prev =>
        {
            if (prev.Status == TaskStatus.RanToCompletion)
                task(prev.Result);
            return prev;
        })
        .Unwrap();

    /// <summary>
    /// Pass <paramref name="task"/> through, after applying <paramref name="Completed"/> (action with side-effects) using its <typeparamref name="T"/> result
    /// or after apply <paramref name="Faulted"/> to it's exception.
    /// </summary>
    public static Task<T> Tee<T>(this Task<T> task, Action<Exception> Faulted, Action<T> Completed)
        => task.ContinueWith
        (
            prev =>
            {
                if (prev.Status == TaskStatus.Faulted)
                    Faulted(prev.Exception!);
                else if (prev.Status == TaskStatus.Canceled)
                    Faulted(new TaskCanceledException());
                else
                    Completed(prev.Result);
                return prev;
            }
        )
        .Unwrap();

    /// <summary>
    /// Pass <paramref name="task"/> through, after applying <paramref name="Completed"/> (action with side-effects) using its <typeparamref name="T"/> result
    /// or after apply <paramref name="Faulted"/> to it's exception.
    /// </summary>
    public static Task<T> TeeAsync<T>(this Task<T> task, Func<Exception, Task> Faulted, Func<T, Task> Completed)
        => task.ContinueWith
        (
            prev =>
            {
                if (prev.Status == TaskStatus.Faulted)
                    Faulted(prev.Exception!);
                else if (prev.Status == TaskStatus.Canceled)
                    Faulted(new TaskCanceledException());
                else
                    Completed(prev.Result);
                return prev;
            }
        )
        .Unwrap();
}