using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace contoso.utility.compression;

internal static class CodePath
{
    
    public static void AssertNotNull<T>([AllowNull][NotNullWhen(true)] T item, string? itemName = null, [CallerMemberName] string? methodName = "")
        where T : class
    {
        if (item is null)
            throw new UnexpectedNullReferenceException($"{methodName}: {itemName ?? "object"} is null. This is unexpected.");
    }

    public static void AssertIsNotNullOrDefault<T>([AllowNull][NotNullWhen(true)] T item, string? itemName = null, [CallerMemberName] string? methodName = "")
    {
        if (EqualityComparer<T>.Default.Equals(item, default(T)))
            throw new UnexpectedNullReferenceException($"{methodName}: {itemName ?? "item"} is null or default. This is unexpected.");
    }

    //public static void AssertNotEmptyErrorFlag(ErrorValue errorFlag, string? itemName = null, [CallerMemberName] string? methodName = "")
    //{
    //    if (EqualityComparer<ErrorValue>.Default.Equals(errorFlag, default(ErrorValue)))
    //        throw new ArgumentException($"{methodName}: {itemName ?? nameof(ErrorValue)} is default. This is unexpected.");
    //}

}

