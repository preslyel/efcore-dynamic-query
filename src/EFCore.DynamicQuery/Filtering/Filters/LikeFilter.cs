using System.Linq.Expressions;
using System.Reflection;
using EFCore.DynamicQuery.Internal;
using Microsoft.EntityFrameworkCore;

namespace EFCore.DynamicQuery.Filtering.Filters;

public sealed class LikeFilter : FilterItem
{
    // Resolved once per process rather than re-looked-up on every filter instance - a FilterItem
    // is deserialized fresh per filter clause in an incoming request.
    private static readonly PropertyInfo FunctionsProperty = typeof(EF).GetProperty(nameof(EF.Functions))!;
    private static readonly MethodInfo LikeMethod = typeof(DbFunctionsExtensions).GetMethod(
        nameof(DbFunctionsExtensions.Like), [typeof(DbFunctions), typeof(string), typeof(string)])!;

    public string? Value { get; set; }
    public override string FilterKey => "like";

    public override Expression GetBinaryExpression(Expression property)
    {
        if (Value is null)
            return LinqExpressionHelper.GetEqualityBinaryExpression(property, Expression.Constant(null), isEqual: true);

        var searchPattern = Expression.Constant(Value);
        var functions = Expression.Property(null, FunctionsProperty);

        var likeCall = Expression.Call(null, LikeMethod, functions, property, searchPattern);
        return Expression.Equal(Expression.Constant(true), likeCall);
    }
}
