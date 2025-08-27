namespace contoso.functional;

#pragma warning disable CS1591
public static partial class Validation
{
    /// <summary>A validation in the invalid state without requiring an explicit generic type.</summary>
    public struct Invalid
    {
        IEnumerable<Error>? _errors = null;
        public Invalid(IEnumerable<Error> errors) => Errors = errors;
        internal IEnumerable<Error> Errors
        {
            get => _errors ??= [Error.Default];
            init => _errors = value;
        }
    }
}
#pragma warning restore CS1591
