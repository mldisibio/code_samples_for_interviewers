using System.Diagnostics.CodeAnalysis;

namespace contoso.utility.fluentextensions;

/// <summary>Format an elapsed milliseconds value for display.</summary>
public readonly struct ElapsedDisplay : IEquatable<ElapsedDisplay>
{
    readonly string _formatted;
    readonly int _hash;

    /// <summary>Format an elapsed milliseconds value for display.</summary>
    public ElapsedDisplay(long? ms, bool omitFraction = false)
    {
        if (ms.HasValue)
        {
            ElapsedMs = ms.Value;
            var timeSpan = TimeSpan.FromMilliseconds(Math.Abs(ms.Value));
            if (omitFraction)
                _formatted = timeSpan.TotalMilliseconds < 60000 ? $"{timeSpan:ss}" : timeSpan.TotalMilliseconds < 86400000 ? $"{timeSpan:hh\\:mm\\:ss}" : $"{timeSpan:d\\.hh\\:mm\\:ss}";
            else
                _formatted = timeSpan.TotalMilliseconds < 60000 ? $"{timeSpan:ss\\.fff}" : timeSpan.TotalMilliseconds < 86400000 ? $"{timeSpan:hh\\:mm\\:ss\\.fff}" : $"{timeSpan:d\\.hh\\:mm\\:ss\\.fff}";

            _hash = HashCode.Combine(ms, omitFraction);
        }
        else
        {
            ElapsedMs = null;
            _formatted = string.Empty;
            _hash = 0;
        }
    }

    /// <summary>Original value of elapsed milliseconds.</summary>
    public long? ElapsedMs { get; }

    /// <summary>True if <see cref="ElapsedMs"/> is not null.</summary>
    public bool HasValue => ElapsedMs.HasValue;

    /// <summary>Verbose display of original and formatted value.</summary>
    public string FullDisplay => HasValue ? $"[{ElapsedMs:N0} = {_formatted}]" : $"[(null) = {_formatted}]";

    /// <summary>Formatted display of <see cref="ElapsedMs"/>.</summary>
    public override string ToString() => _formatted;

    /// <inheritdoc/>
    public override int GetHashCode() => _hash;

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj) => (obj is ElapsedDisplay other) && Equals(other);

    /// <summary>True if both formatted strings are exactly equal.</summary>
    public bool Equals(ElapsedDisplay other) => string.Equals(_formatted, other._formatted);

    /// <summary>True if both formatted strings exactly equal.</summary>
    public static bool operator ==(ElapsedDisplay left, ElapsedDisplay right) => left.Equals(right);

    /// <summary>True if both formatted strings are not exactly equal.</summary>
    public static bool operator !=(ElapsedDisplay left, ElapsedDisplay right) => !(left == right);

    /// <summary>Implicit conversion from <see cref="ElapsedDisplay"/> to string.</summary>
    public static implicit operator string(ElapsedDisplay elapsed) => elapsed._formatted;

    /// <summary>Cast <paramref name="ms"/> to <see cref="ElapsedDisplay"/>.</summary>
    public static implicit operator ElapsedDisplay(long? ms) => new ElapsedDisplay(ms);
}
