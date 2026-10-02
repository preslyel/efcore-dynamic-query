using System.Linq.Expressions;
using EFCore.DynamicQuery.Exceptions;
using EFCore.DynamicQuery.Internal;
using EFCore.DynamicQuery.Mapping;
using EFCore.DynamicQuery.Querying.Internal;

namespace EFCore.DynamicQuery.Querying;

public static class OrderingQueryExtensions
{
    /// <summary>
    /// Orders by a mapped property, by name. When the mapped expression is collection-valued
    /// (e.g. ordering a parent by the smallest value across a child collection), orders by the
    /// minimum value across that collection instead of failing to compile a key selector.
    /// </summary>
    public static IQueryable<TEntity> GetOrderedData<TEntity>(
        this IQueryable<TEntity> query, string orderBy, bool ascending, ITypeMap typeMap, bool applyTransforms = false)
    {
        if (string.IsNullOrEmpty(orderBy))
            return query;

        var sourceProperty = typeMap.GetEffectiveSourceProperty(orderBy, applyTransforms) ?? throw new PropertyNotMappedException(orderBy);
        var parameter = Expression.Parameter(typeof(TEntity), "x");
        var memberExpression = ExpressionParameterHelper.ReplaceRootParameter(parameter, sourceProperty);

        var keySelector = FilterExpressionBuilder.IsCollectionExpression(memberExpression)
            ? BuildMinOfCollectionSelector(parameter, memberExpression)
            : memberExpression;

        var orderByLambda = Expression.Lambda(keySelector, parameter);
        var methodName = ascending ? nameof(Queryable.OrderBy) : nameof(Queryable.OrderByDescending);

        var orderByCall = Expression.Call(
            typeof(Queryable),
            methodName,
            [typeof(TEntity), orderByLambda.Body.Type],
            query.Expression,
            Expression.Quote(orderByLambda));

        return query.Provider.CreateQuery<TEntity>(orderByCall);
    }

    private static Expression BuildMinOfCollectionSelector(ParameterExpression parameter, Expression memberExpression)
    {
        var chain = FilterExpressionBuilder.UnrollChain(memberExpression);

        var navRoot = new SafeRootParameterReplacer(parameter, parameter).Visit(chain.Root);
        var elementType = navRoot.Type.GetGenericArguments()[0];
        var elementParameter = Expression.Parameter(elementType, "e");

        var lastSelector = chain.Selectors[^1];
        var selectorBody = new SafeRootParameterReplacer(lastSelector.Parameters[0], elementParameter).Visit(lastSelector.Body);
        var innerLambda = Expression.Lambda(selectorBody, elementParameter);

        var minMethod = typeof(Enumerable)
            .GetMethods()
            .First(m => m.Name == nameof(Enumerable.Min) && m.GetParameters().Length == 2 && m.GetGenericArguments().Length == 2)
            .MakeGenericMethod(elementType, innerLambda.ReturnType);

        return Expression.Call(minMethod, navRoot, innerLambda);
    }
}
