using System.Diagnostics.CodeAnalysis;

namespace contoso.utility.ioabstracted;

/// <summary>Validates a string as non-empty, free of invalid file name characters, and either has alphanumeric content or is a unix root.</summary>
public readonly struct IOPath : IEquatable<IOPath>
{
    readonly int _hash;
    readonly string? _value;

    /// <summary>Validates a string as non-empty, free of invalid file name characters, and either has alphanumeric content or is a unix root.</summary>
    public IOPath(string path)
    {
        if (!path.AppearsValidPath(out string? error))
        {
            IsValid = false;
            Error = error;
            _value = path;
            _hash = _value == null ? 0 : _value.ToLowerInvariant().GetHashCode();
        }
        else
        {
            IsValid = true;
            Error = null;
            _value = path.Trim();
            _hash = _value.ToLowerInvariant().GetHashCode();
        }
    }

    /// <summary>The submitted path value, trimmed of whitespace.</summary>
    public string Value => _value ?? string.Empty;

    /// <summary>True if submitted string was parsed as a file name or path without error. Does not mean file or path exists.</summary>
    public bool IsValid { get; init; }

    /// <summary>Any error message when validating the string submitted as a path, or null if no exception was encountered.</summary>
    public string? Error { get; init; }

    /// <inheritdoc/>
    public override string ToString() => Value;

    /// <inheritdoc/>
    public override int GetHashCode() => _hash;

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj) => (obj is IOPath other) && Equals(other);

    /// <summary>True if both fully qualified absolute paths valid and exactly equal.</summary>
    public bool Equals(IOPath other) => string.Equals(_value, other._value, StringComparison.InvariantCulture);

    /// <summary>True if both fully qualified absolute paths valid and equal, case-insensitive.</summary>
    public bool EqualsIgnoringCase(IOPath other) => string.Equals(_value, other._value, StringComparison.InvariantCultureIgnoreCase);

    /// <summary>True if both fully qualified absolute paths valid and exactly equal.</summary>
    public static bool operator ==(IOPath left, IOPath right) => left.Equals(right);

    /// <summary>True if either path is invalid or both paths are not exactly equal.</summary>
    public static bool operator !=(IOPath left, IOPath right) => !(left == right);

    /// <summary>Implicit conversion from <see cref="IOPath"/> to string.</summary>
    public static implicit operator string(IOPath path) => path.Value;

    /// <summary>Cast <paramref name="str"/> to <see cref="IOPath"/> if not null or empty.</summary>
    public static implicit operator IOPath(string str) => new IOPath(str);

}
