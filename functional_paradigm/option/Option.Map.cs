using static contoso.functional.FnConstructs;

namespace contoso.functional
{

    public static partial class Option
    {
        /// <summary>No-op mapping in an elevated context.</summary>
        public static Option<TOut> Map<TIn, TOut>(this NoValue _, Func<TIn, TOut> map)
            => None;

        /// <summary>Apply <paramref name="map"/> to the inner value of <paramref name="src"/> and return the result as <see cref="Option{TOut}"/></summary>
        /// <example>
        /// <code>
        /// Option{Cat} whiskers = Some{Cat}(new(Name: "Whiskers", Age: 3));
        /// Option{string} catName = whiskers.Map(cat => cat.Name); // catName contains "Whiskers"
        /// </code>
        /// </example>
        public static Option<TOut> Map<TIn, TOut>(this Option<TIn> src, Func<TIn, TOut> map)
            => src.Match
            (
               None: () => None,
               Some: (t) => Some(map(t))
            );
    }
}


namespace contoso.functional.advanced
{
    /// <summary></summary>
    public static partial class OptionAdvanced
    {
        /// <summary>Return an unary function expecting <typeparamref name="T2"/> with the inner value of <paramref name="optionT1"/> partially applied as the first argument of <paramref name="binaryMap"/>.</summary>
        public static Option<Func<T2, TOut>> Map<T1, T2, TOut>(this Option<T1> optionT1, Func<T1, T2, TOut> binaryMap)
            => optionT1.Map(binaryMap.Curry());

        /// <summary>
        /// Return an binary function expecting <typeparamref name="T2"/> and <typeparamref name="T3"/> 
        /// with the inner value of <paramref name="optionT1"/> partially applied as the first argument of <paramref name="ternaryMap"/>.
        /// </summary>
        public static Option<Func<T2, T3, TOut>> Map<T1, T2, T3, TOut>(this Option<T1> optionT1, Func<T1, T2, T3, TOut> ternaryMap)
            => optionT1.Map(ternaryMap.CurryFirst());
    }
}

