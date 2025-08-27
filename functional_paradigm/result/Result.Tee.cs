using Unit = System.ValueTuple;

namespace contoso.functional;

public static partial class Result
{
    /// <summary>
    /// Execute <paramref name="action"/> if <paramref name="src"/> represents a success of <typeparamref name="T"/>, otherwise do nothing.
    /// Returns a valid but empty instance if <paramref name="src"/> is success, otherwise the original exception
    /// </summary>
    public static Result<Unit> ForEach<T>(this Result<T> src, Action<T> action)
        => Map(src, action.ToFunc());

    /// <summary>
    /// Execute <paramref name="action"/> if <paramref name="src"/> represents a success of <typeparamref name="T"/>, otherwise do nothing.
    /// Returns <paramref name="src"/> as is.
    /// </summary>
    public static Result<T> Do<T>(this Result<T> src, Action<T> action)
    {
        src.ForEach(action);
        return src;
    }
}