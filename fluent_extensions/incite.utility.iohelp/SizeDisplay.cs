using System.Diagnostics.CodeAnalysis;

namespace contoso.utility.iohelp;

/// <summary>Human readable display of a value representing a length or size in bytes.</summary>
public readonly struct SizeDisplay : IEquatable<SizeDisplay>
{
    readonly string _formatted;
    readonly int _hash;

    /// <summary>Ctor</summary>
    /// <param name="bytes">Value to format for display representing a count or length in bytes.</param>
    /// <param name="tagIfEmpty">Optional string to display if <paramref name="bytes"/> has no value.</param>
    public SizeDisplay(long? bytes, string tagIfEmpty = "N/A")
    {
        Bytes = bytes;
        _formatted = bytes.HasValue ? bytes.Value.ToFormattedSizeDisplay() : tagIfEmpty ?? "N/A";
        _hash = HashCode.Combine(bytes, tagIfEmpty);
    }

    /// <summary>Original length or size value.</summary>
    public long? Bytes { get; }

    /// <summary>True if <see cref="Bytes"/> is not null.</summary>
    public bool HasValue => Bytes.HasValue;

    /// <summary>Verbose display of original and formatted value.</summary>
    public string FullDisplay => HasValue ? $"[{Bytes:N0} = {_formatted}]" : $"[(null) = {_formatted}]";

    /// <summary>Formatted display of <see cref="Bytes"/>.</summary>
    public override string ToString() => _formatted;

    /// <inheritdoc/>
    public override int GetHashCode() => _hash;

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj) => (obj is SizeDisplay other) && Equals(other);

    /// <summary>True if both formatted strings are exactly equal.</summary>
    public bool Equals(SizeDisplay other) => string.Equals(_formatted, other._formatted);

    /// <summary>True if both formatted strings exactly equal.</summary>
    public static bool operator ==(SizeDisplay left, SizeDisplay right) => left.Equals(right);

    /// <summary>True if both formatted strings are not exactly equal.</summary>
    public static bool operator !=(SizeDisplay left, SizeDisplay right) => !(left == right);

    /// <summary>Implicit conversion from <see cref="SizeDisplay"/> to string.</summary>
    public static implicit operator string(SizeDisplay size) => size._formatted;

    /// <summary>Cast <paramref name="size"/> to <see cref="SizeDisplay"/>.</summary>
    public static implicit operator SizeDisplay(long? size) => new SizeDisplay(size);
}
