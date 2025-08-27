namespace contoso.functional;

public static partial class TaskContainer
{
    static readonly Action _doNothing = () => { };

    /// <summary>
    /// Returns <paramref name="src"/> as a <see cref="Task{T}"/> in the Completion state if Some and <paramref name="validate"/> is true,
    /// otherwise in the exception state with an <see cref="ArgumentNullException"/> if None or with a <see cref="ArgumentException"/> of <paramref name="error"/>
    /// if <paramref name="validate"/> returns false.
    /// </summary>
    public static Task<T> AsyncValidIf<T>(this Option<T> src, Predicate<T> validate, Error error)
        => src.Match
        (
            None: () => Task.FromException<T>(new ArgumentNullException(nameof(src))),
            Some: t => validate(t) ? Task.FromResult(t) : Task.FromException<T>(new ArgumentException(error))
        );
}
