using System.Diagnostics.CodeAnalysis;

namespace contoso.functional;

/// <summary>Represents a non-empty string that can be strongly typed as an error.</summary>
public struct Error : IEquatable<Error>
{
    const string _unspecified = "Unspecified Error";
    string? _value = null;

    Error(string error) => Value = error;

    string Value
    {
        get => _value ??= _unspecified;
        init => _value = value;
    }

    /// <summary>Default value of 'ErrorValue'.</summary>
    public static Error Default => new(_unspecified);

    /// <summary>Create an <see cref="Error"/> from a non-empty string.</summary>
    public static Error Of(string error) => string.IsNullOrEmpty(error) ? Default : new(error);

    /// <summary>The underlying value, or an empty string if uninitialized.</summary>
    [return: NotNull]
    public override readonly string ToString() => this;

    /// <summary>Hash of the underlying value.</summary>
    public override int GetHashCode() => Value.GetHashCode();

    /// <summary>True if two instances are equal, case-insensitive.</summary>
    public override bool Equals([NotNullWhen(true)] object? obj) => (obj is Error other) && Equals(other);

    /// <summary>True if two instances are equal, case-insensitive.</summary>
    public bool Equals(Error other) => string.Equals(this.Value, other.Value, StringComparison.OrdinalIgnoreCase);

    /// <summary>True if two instances are equal, case-insensitive.</summary>
    public static bool operator ==(Error left, Error right) => left.Equals(right);

    /// <summary>True if two instances are not equal, case-insensitive.</summary>
    public static bool operator !=(Error left, Error right) => !(left == right);

    /// <summary>Implicit conversion from <see cref="Error"/> to string.</summary>
    public static implicit operator string(Error err) => err.Value;

    /// <summary>Cast <paramref name="str"/> to <see cref="Error"/> if not null or empty.</summary>
    public static explicit operator Error(string str) => Of(str);
}
