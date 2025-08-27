using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace contoso.utility.fluentextensions;
/// <summary>
/// Extension which allows binary predicates (AndAlso/OrElse) to be dynamically composed and chained,
/// primarily for LINQ providers, where a predicate is sent over the wire and transformed to another 
/// language, such as SQL.
/// </summary>
/// <remarks>See: http://www.albahari.com/nutshell/predicatebuilder.aspx .</remarks>
public static class PredicateBuilder
{
    // ----------------------------------------------------------------------------------
    // 'True<T>()' and 'False<T>()' are shortcuts for creating Expression<Func<T, bool>>
    // so that this:
    //    Expression<Func<Product, bool>> predicate = c => true;
    // can be written as:
    //    var predicate = PredicateBuilder.True<Product>();
    // These allow a composed predicate to work even when no predicate items are supplied
    // by allowing for a 'true' or 'false' starting point.
    // Use 'False<T>()' for starting a series of Or's and use 'True<T>()' for a series of And's.
    //
    // a lambda function (the definition of the anonymous method) can be transformed by the compiler into a delegate or into an Expression
    // - for in-memory, the cpu executes the delegate against an IEnumerable
    // - for db context, the compiler treats the delegate as an Expression and translates the Expression into sql
    //
    // an Expression can be translated/compiled into a delegate
    //   Expression<Func<int, bool>> exp = x => x == 42;
    //   Func<int,bool> func = exp.Compile(); // which you need to do if you are using an Expression for both in-memory (client validation) and sql (server execution)
    //                                        // and are applying it to the in-memory IEnumerable
    //
    // but a delegate cannot be translated/compiled into an expression
    //   Func<int,bool> func = x => x == 42;
    //   Expression<Func<int, bool>> exp = func.?
    //
    // ----------------------------------------------------------------------------------

    /// <summary>Creates a default starting point for zero or more 'AndAlso' predicates.</summary>
    public static Expression<Func<T, bool>> True<T>() => t => true;

    /// <summary>Creates a default starting point for zero or more 'OrElse' predicates.</summary>
    public static Expression<Func<T, bool>> False<T>() => t => false;

    /// <summary>Chain an 'OrElse' BinaryExpression to the underlying source expression with another supplied expression.</summary>
    public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        // Because these are static extensions starting with our True<T> or False<T>, the compiler won't infer
        // the instance of T as a 'parameter' the same way it does when if these were chained 'instance' methods instead;

        //BinaryExpression orExpression = Expression.OrElse(left.Body, right.Body);
        //return Expression.Lambda<Func<T, bool>>(orExpression, left.Parameters.Single());


        // So setup an (Invocation)Expression which will call the second expression with the first expression's parameters;
        // This ensures that the parameter applied to the second is the same as the parameter applied to the first.
        // In essence, for (t1 => t1.something) and (t2 => t2.somethingelse), force t1 and t2 to be the same.
        // For Func<T, bool>, t1 and t2 are instances of T, and should be the same instance.
        var rightInvokedWLeftParam = Expression.Invoke(right, left.Parameters.Cast<Expression>());
        BinaryExpression orExpression = Expression.OrElse(left.Body, rightInvokedWLeftParam);
        return Expression.Lambda<Func<T, bool>>(orExpression, left.Parameters.Single());
    }

    /// <summary>Chain an 'AndAlso' BinaryExpression to the underlying source expression with another supplied expression.</summary>
    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        var rightInvokedWithLeftParam = Expression.Invoke(right, left.Parameters.Cast<Expression>());
        BinaryExpression andExpression = Expression.AndAlso(left.Body, rightInvokedWithLeftParam);
        return Expression.Lambda<Func<T, bool>>(andExpression, left.Parameters.Single());
    }

    /// <summary>
    /// Returns the complement of a predicate expression;
    /// i.e. for predicate <c>a(x)</c> returns an expression yielding <c>!a(x)</c>.
    /// </summary>
    /// <remarks>
    /// Most contributors call this 'Not{T}()' but since it is an extension method, that
    /// makes for awkward syntax: <c>something.And(somethingElse.Not())</c>
    /// </remarks>
    public static Expression<Func<T, bool>> Complement<T>(this Expression<Func<T, bool>> expr)
    {
        var negatedExpr = Expression.Not(expr.Body);
        return Expression.Lambda<Func<T, bool>>(negatedExpr, expr.Parameters.Single());
    }

    /// <summary>Convert a given predicate expression into an actual <see cref="Predicate{T}"/>.</summary>
    [return: NotNull]
    public static Predicate<T> ToPredicate<T>(this Expression<Func<T, bool>>? boolExpression)
    {
        if (boolExpression == null)
            return t => false;
        // see https://stackoverflow.com/a/1218280/458354
        // Compile() yields Func<T, bool> func
        // and func.Invoke yields Predicate<T>
        return new Predicate<T>(boolExpression.Compile());
    }
}
