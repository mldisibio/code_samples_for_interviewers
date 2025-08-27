using Unit = System.ValueTuple;

namespace contoso.functional;

public static partial class Either
{
    /// <summary>Execute <paramref name="actionOnR"/> if <paramref name="src"/> represents an <typeparamref name="R"/>, otherwise do nothing.</summary>
    public static Either<L, Unit> ForEach<L, R>(this Either<L, R> src, Action<R> actionOnR)
        => Map(src, actionOnR.ToFunc());
}