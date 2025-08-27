using System.Diagnostics.CodeAnalysis;

namespace contoso.utility.fluentextensions;
/// <summary>
/// Extension which allows <see cref="Predicate{T}"/> functions to be dynamically composed and chained,
/// primarily for LINQ to Objects, where a collection needs to be filtered by one or more predicates.
/// </summary>
/// <remarks> 
/// See: http://www.chapleau.info/blog/2010/09/27/predicate-builder.html .
/// This only works for in-process LINQ queries. Out-of-process queries, such as LINQ to SQL, 
/// need to make use of Expressions (see <see cref="PredicateBuilder"/>).
/// </remarks>
public static class PredicateComposer
{
    /// <summary>Creates a default starting point for a series of zero or more 'And' predicates.</summary>
    public static Predicate<T> True<T>() => new Predicate<T>(_ => true);

    /// <summary>Creates a default starting point for a series of zero or more 'Or' predicates.</summary>
    public static Predicate<T> False<T>() => new Predicate<T>(_ => false);

    /// <summary>Chain a second <see cref="Predicate{T}"/> to the underlying source predicate using a bool 'Or'.</summary>
    [return: NotNull]
    public static Predicate<T> Or<T>(this Predicate<T> lhs, Predicate<T>? rhs) 
    {
        if (lhs == null)
            return rhs ?? False<T>();

        return rhs == null ? lhs : new Predicate<T>(x => lhs(x) || rhs(x));
    }

    /// <summary>Chain a second <see cref="Predicate{T}"/> to the underlying source predicate using a bool 'And'.</summary>
    [return: NotNull]
    public static Predicate<T> And<T>(this Predicate<T> lhs, Predicate<T>? rhs)
    {
        if (lhs == null)
            return rhs ?? False<T>();

        return rhs == null ? lhs : new Predicate<T>(x => lhs(x) && rhs(x));
    }

    /// <summary>Chain a one or more <see cref="Predicate{T}"/> to the underlying source predicate using a bool 'Or' between each.</summary>
    /// <remarks>If no arguments are supplied, the original predicate is returned.</remarks>
    [return: NotNull]
    public static Predicate<T> OrAnyOf<T>(this Predicate<T> lhs, params Predicate<T>[] rhs)
    {
        if (rhs == null || rhs.Length == 0)
            return lhs ?? False<T>();

        Predicate<T> tempL = lhs ?? False<T>();
        foreach (var tempR in rhs)
        {
            Predicate<T> prevL = tempL;
            Predicate<T> nextR = tempR;
            tempL = prevL.Or(nextR);
        }
        return tempL;
    }

    /// <summary>Chain a one or more <see cref="Predicate{T}"/> to the underlying source predicate using a bool 'And' between each.</summary>
    /// <remarks>If no arguments are supplied, the original predicate is returned.</remarks>
    [return: NotNull]
    public static Predicate<T> AndEachOf<T>(this Predicate<T> lhs, params Predicate<T>[] rhs)
    {
        if (rhs == null || rhs.Length == 0)
            return lhs ?? False<T>();

        Predicate<T> tempL = lhs ?? False<T>();
        foreach (var tempR in rhs)
        {
            Predicate<T> prevL = tempL;
            Predicate<T> nextR = tempR;
            tempL = prevL.And(nextR);
        }
        return tempL;
    }

    /// <summary>
    /// Returns the complement of a <see cref="Predicate{T}"/> expression;
    /// i.e. for any predicate <c>f(x)</c> returns a new Predicate yielding <c>!f(x)</c>.
    /// </summary>
    /// <remarks>
    /// Most contributors call this 'Not{T}()' but since it is an extension method, that
    /// makes for awkward syntax: <c>something.And(somethingElse.Not())</c>
    /// </remarks>
    [return: NotNull]
    public static Predicate<T> Complement<T>(this Predicate<T> predicate)
    {
        return predicate == null ? False<T>() : new Predicate<T>(x => !predicate(x));
    }

    /// <summary>Chain a second <see cref="Predicate{T}"/> to the underlying source predicate using a bool 'And Not'.</summary>
    [return: NotNull]
    public static Predicate<T> AndNot<T>(this Predicate<T> lhs, Predicate<T>? rhs)
    {
        if (lhs == null)
            return rhs ?? False<T>();

        // return new Predicate<T>(x => left(x) && !right(x));
        return rhs == null ? lhs : lhs.And(rhs.Complement());
    }
}
