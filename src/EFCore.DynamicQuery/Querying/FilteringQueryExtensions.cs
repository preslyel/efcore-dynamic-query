using System.Linq.Expressions;
using EFCore.DynamicQuery.Exceptions;
using EFCore.DynamicQuery.Filtering;
using EFCore.DynamicQuery.Filtering.Filters;
using EFCore.DynamicQuery.Internal;
using EFCore.DynamicQuery.Mapping;
using EFCore.DynamicQuery.Querying.Internal;

namespace EFCore.DynamicQuery.Querying;

public static class FilteringQueryExtensions
{
    public static IQueryable<TEntity> GetFilteredData<TEntity>(
        this IQueryable<TEntity> query, IFilterItem[] filterItems, ITypeMap typeMap, bool applyTransforms = false)
    {
        var parameter = Expression.Parameter(typeof(TEntity), "x");
        var combined = GetFilterExpression<TEntity>(filterItems, typeMap, parameter, applyTransforms);

        return combined is null ? query : query.Where(Expression.Lambda<Func<TEntity, bool>>(combined, parameter));
    }

    private static Expression? GetFilterExpression<TEntity>(
        IFilterItem[] filterItems, ITypeMap typeMap, ParameterExpression parameter, bool applyTransforms)
    {
        Expression? combined = null;
        Expression Accumulate(Expression next) => combined is null ? next : Expression.AndAlso(combined, next);

        foreach (var filter in filterItems)
        {
            var sourceProperty = typeMap.GetEffectiveSourceProperty(filter.Name, applyTransforms) ?? throw new PropertyNotMappedException(filter.Name);
            var memberExpression = ExpressionParameterHelper.ReplaceRootParameter(parameter, sourceProperty);

            if (memberExpression is MethodCallExpression methodCall && FilterExpressionBuilder.IsSimplePropertyJoin(methodCall))
            {
                var joinExpression = filter.GetBinaryExpression(BuildJoinedPropertyExpression(parameter, methodCall));
                combined = Accumulate(joinExpression);
                continue;
            }

            // NamedEqualFilter always operates against the whole object it's given (ANDing
            // several sibling properties), not a single mapped property - it needs the same
            // navigation-chain unrolling BuildAnyPredicate does even when the mapped expression
            // itself isn't detected as a collection.
            if (FilterExpressionBuilder.IsCollectionExpression(memberExpression) || filter is NamedEqualFilter)
            {
                var anyPredicate = FilterExpressionBuilder.BuildAnyPredicate<TEntity>(memberExpression, filter, parameter);
                combined = Accumulate(anyPredicate.Body);
                continue;
            }

            var expression = filter.GetBinaryExpression(memberExpression);
            combined = Accumulate(expression);
        }

        return combined;
    }

    private static Expression BuildJoinedPropertyExpression(ParameterExpression parameter, MethodCallExpression joinCall)
    {
        var properties = FilterExpressionBuilder.ExtractProperties(joinCall);
        var separator = FilterExpressionBuilder.ExtractSeparator(joinCall);
        var concatMethod = typeof(string).GetMethod(nameof(string.Concat), [typeof(string), typeof(string)])!;

        Expression? concatenated = null;

        foreach (var property in properties)
        {
            var replacedProperty = ExpressionParameterHelper.ReplaceRootParameter(parameter, property);
            var safeProperty = Expression.Coalesce(replacedProperty, Expression.Constant(string.Empty));

            if (concatenated is null)
            {
                concatenated = safeProperty;
                continue;
            }

            var withSeparator = Expression.Add(concatenated, Expression.Constant(separator), concatMethod);
            concatenated = Expression.Add(withSeparator, safeProperty, concatMethod);
        }

        return concatenated!;
    }
}
