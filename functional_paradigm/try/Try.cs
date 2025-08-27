using System.Runtime.CompilerServices;

namespace contoso.functional;


/// <summary>Signature representing any operation that may throw an exception.</summary>
public delegate Result<T> Try<T>();

public static partial class TryCatch
{
    /// <summary>Invoke the operation wrapped by <paramref name="op"/> within a try/catch and return <typeparamref name="T"/> or any caught exception as <see cref="Result{T}"/>.</summary>
    public static Result<T> Run<T>(this Try<T> op, Option<Func<string>> addErrorContext = default, [CallerMemberName] string? calledFrom = "")
    {
        try { return op(); }
        catch (Exception ex)
        {
            return addErrorContext.Match
                   (
                       None: () => Result.Of<T>(ex, calledFrom: calledFrom),
                       Some: fn => Result.Of<T>(ex, context: fn(), calledFrom: calledFrom)
                   );
        }
    }
}