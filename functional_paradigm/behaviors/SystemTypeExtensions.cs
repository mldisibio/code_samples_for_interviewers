using static contoso.functional.FnConstructs;

namespace contoso.functional;

#pragma warning disable CS1591
public static class FnSystem
{

    public static Option<T> Maybe<T>(this Nullable<T> src) where T : struct => src.HasValue ? Some(src.Value) : None;

    public static Option<V> Maybe<K, V>(this IDictionary<K, V> dictionary, K key) => dictionary.TryGetValue(key, out V? value) ? Some(value) : None;
}

public static class DateTimeOption
{
    public static Option<DateTime> Maybe(string s) => DateTime.TryParse(s, out DateTime result) ? Some(result) : None;
}

public static class DecimalOption
{
    public static Option<decimal> Maybe(string s) => decimal.TryParse(s, out decimal result) ? Some(result) : None;
}

public static class DoubleOption
{
    public static Option<double> Maybe(string s) => double.TryParse(s, out double result) ? Some(result) : None;

    public static bool IsOdd(decimal i) => i % 2 == 1;

    public static bool IsEven(decimal i) => i % 2 == 0;

    public static readonly Func<decimal, string> Stringify = d => d.ToString();
}

public static class EnumOption
{
    public static Option<TEnum> Maybe<TEnum>(string s, bool ignoreCase = false) where TEnum : struct
        => Enum.TryParse(s, ignoreCase: ignoreCase, out TEnum t) ? Some(t) : None;
}

public static class Int32Option
{
    public static Option<int> Maybe(string s) => int.TryParse(s, out int result) ? Some(result) : None;

    public static readonly Func<int, string> Stringify = i => i.ToString();
}

public static class Int64Option
{
    public static Option<long> Maybe(string s) => long.TryParse(s, out long result) ? Some(result) : None;

    public static readonly Func<long, string> Stringify = n => n.ToString();
}

public static class FnString
{
    /// <summary><see cref="String.Trim()"/> as a delegate.</summary>
    public static readonly Func<string, string> Trim = s => s.Trim();
    /// <summary><see cref="String.ToLower()"/> as a delegate.</summary>
    public static readonly Func<string, string> ToLower = s => s.ToLower();
    /// <summary><see cref="String.ToUpper()"/> as a delegate.</summary>
    public static readonly Func<string, string> ToUpper = s => s.ToUpper();
}
#pragma warning restore CS1591