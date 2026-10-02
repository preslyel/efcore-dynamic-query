using System.Linq.Expressions;
using EFCore.DynamicQuery.Internal;

namespace EFCore.DynamicQuery.Querying;

/// <summary>
/// Strongly-typed "property is/isn't in this set of values" filtering - a compile-time-known-property
/// complement to <see cref="Filtering.Filters.InFilter"/>'s dynamic, name-driven equivalent.
/// </summary>
public static class WhereInQueryExtensions
{
    public static IQueryable<TEntity> WhereIn<TEntity, TProperty, TValue>(
        this IQueryable<TEntity> source,
        Expression<Func<TEntity, TProperty>> property,
        IEnumerable<TValue> values,
        Expression<Func<TEntity, bool>>? orCondition = null)
        where TEntity : class
    {
        var valuesList = values?.ToList() ?? [];
        if (valuesList.Count == 0)
            return source.Where(_ => false);

        var parameter = Expression.Parameter(typeof(TEntity), "e");
        var propertyExpression = ExpressionParameterHelper.ReplaceRootParameter(parameter, property.Body);

        var containsExpression = BuildContainsExpression(propertyExpression, valuesList);

        return ComputeInQuery(source, parameter, containsExpression, orCondition);
    }

    public static IQueryable<TEntity> WhereNotIn<TEntity, TProperty, TValue>(
        this IQueryable<TEntity> source,
        Expression<Func<TEntity, TProperty>> property,
        IEnumerable<TValue> values,
        Expression<Func<TEntity, bool>>? orCondition = null)
        where TEntity : class
    {
        var valuesList = values?.ToList() ?? [];
        if (valuesList.Count == 0)
            return source;

        var parameter = Expression.Parameter(typeof(TEntity), "e");
        var propertyExpression = ExpressionParameterHelper.ReplaceRootParameter(parameter, property.Body);

        var containsExpression = BuildContainsExpression(propertyExpression, valuesList);

        return ComputeInQuery(source, parameter, Expression.Not(containsExpression), orCondition);
    }

    /// <summary>
    /// Builds a <c>List&lt;TValue&gt;.Contains(property)</c> check - matching
    /// <see cref="Filtering.Filters.InFilter"/>'s own approach - instead of an OR-chain of per-value
    /// equality checks, which stops scaling linearly as the value set grows. Guards a nullable
    /// property with a <c>HasValue</c> check before unwrapping, the same as <c>InFilter</c>, so a
    /// null row is excluded from the set rather than throwing.
    /// </summary>
    private static Expression BuildContainsExpression<TValue>(Expression propertyExpression, List<TValue> values)
    {
        var containsMethod = typeof(List<TValue>).GetMethod(nameof(List<TValue>.Contains), [typeof(TValue)])!;
        var valuesConstant = Expression.Constant(values);

        if (propertyExpression.Type == typeof(TValue))
            return Expression.Call(valuesConstant, containsMethod, propertyExpression);

        if (Nullable.GetUnderlyingType(propertyExpression.Type) == typeof(TValue))
        {
            var hasValue = Expression.Property(propertyExpression, "HasValue");
            var containsCall = Expression.Call(valuesConstant, containsMethod, Expression.Property(propertyExpression, "Value"));
            return Expression.AndAlso(hasValue, containsCall);
        }

        return Expression.Call(valuesConstant, containsMethod, Expression.Convert(propertyExpression, typeof(TValue)));
    }

    private static IQueryable<TEntity> ComputeInQuery<TEntity>(
        IQueryable<TEntity> source, ParameterExpression parameter, Expression combined, Expression<Func<TEntity, bool>>? orCondition)
        where TEntity : class
    {
        var body = orCondition is not null
            ? Expression.OrElse(combined, ExpressionParameterHelper.ReplaceRootParameter(parameter, orCondition.Body))
            : combined;

        return source.Where(Expression.Lambda<Func<TEntity, bool>>(body, parameter));
    }
}
