using System.Runtime.CompilerServices;

namespace contoso.functional;

#pragma warning disable CS1591
public static partial class Result
{
    public static Func<T, Result<T>> Return<T>() => t => t;

    public static Result<T> Of<T>(Exception ex, Option<string> context = default, [CallerMemberName] string? calledFrom = "") => new Result<T>(ex, context, calledFrom);

    public static Result<T> Of<T>(in T success, [CallerMemberName] string? calledFrom = "") => new Result<T>(success, calledFrom);

    internal static Result<T> Of<T>(Failure exWrapper)
        => new Result<T>(exWrapper.Exception, exWrapper.Context, exWrapper.CalledFrom.GetValueOr(string.Empty));
}
#pragma warning restore CS1591
