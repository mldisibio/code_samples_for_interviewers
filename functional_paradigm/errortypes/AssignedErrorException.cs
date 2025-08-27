namespace contoso.functional;

/// <summary>An exception wrapping the assigned <see cref="Error"/> from which a Result is created in the failure state.</summary>
[Serializable]
public class AssignedErrorException : Exception
{
    /// <summary>Initialize from the assigned <see cref="Error"/> which set the Result in context to a failure state, and optional caller name and/or descriptive context.</summary>
    public AssignedErrorException(Error errorValue, string? context = null, string? methodName = null) : base(FormatErrorMessage(errorValue, context, methodName)) { }

    static string FormatErrorMessage(in Error error, string? context, string? methodName)
        => string.IsNullOrEmpty(context)
           ? FormatErrorMessage(error, methodName)
           : string.IsNullOrEmpty(methodName)
             ? $"{error} Context: [{context}]"
             : $"{error} CalledFrom: [{methodName}] Context: [{context}";

    static string FormatErrorMessage(in Error error, string? methodName)
        => string.IsNullOrEmpty(methodName)
           ? $"{error}"
           : $"{error} CalledFrom: [{methodName}]";
}
