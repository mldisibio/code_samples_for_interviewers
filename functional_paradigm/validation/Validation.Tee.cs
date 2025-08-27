using Unit = System.ValueTuple;

namespace contoso.functional;

public static partial class Validation
{
    /// <summary>
    /// Execute <paramref name="action"/> if <paramref name="src"/> represents a valid <typeparamref name="T"/>, otherwise do nothing.
    /// Returns a valid but empty instance, or the original errors if already invalid.
    /// </summary>
    public static Validation<Unit> ForEach<T>(this Validation<T> src, Action<T> action)
        => Map(src, action.ToFunc());

    /// <summary>
    /// Execute <paramref name="action"/> if <paramref name="src"/> represents a valid <typeparamref name="T"/>, otherwise do nothing.
    /// Returns <paramref name="src"/> as is.
    /// </summary>
    public static Validation<T> Do<T>(this Validation<T> src, Action<T> action)
    {
        src.ForEach(action);
        return src;
    }
}