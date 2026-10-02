using System.Linq.Expressions;
using EFCore.DynamicQuery.Filtering;
using Microsoft.EntityFrameworkCore;

namespace EFCore.DynamicQuery.Tests.TestSupport;

/// <summary>
/// A from-scratch custom filter, written exactly the way a library consumer would: extend
/// <see cref="FilterItem"/>, override <see cref="FilterKey"/> and <see cref="GetBinaryExpression"/>,
/// then register it - registry.Register&lt;StartsWithFilter&gt;("startsWith"). Proves the
/// extension point works without touching any library source.
/// </summary>
public sealed class StartsWithFilter : FilterItem
{
    public string? Value { get; set; }
    public override string FilterKey => "startsWith";

    public override Expression GetBinaryExpression(Expression property)
    {
        var searchPattern = Expression.Constant(Value + "%");
        var functions = Expression.Property(null, typeof(EF).GetProperty(nameof(EF.Functions))!);
        var likeMethod = typeof(DbFunctionsExtensions).GetMethod(
            nameof(DbFunctionsExtensions.Like), [functions.Type, typeof(string), typeof(string)])!;

        var likeCall = Expression.Call(null, likeMethod, functions, property, searchPattern);
        return Expression.Equal(Expression.Constant(true), likeCall);
    }
}
