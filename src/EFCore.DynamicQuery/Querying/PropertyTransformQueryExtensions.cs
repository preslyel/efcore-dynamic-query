using System.Linq.Expressions;
using System.Reflection;
using EFCore.DynamicQuery.Internal;
using EFCore.DynamicQuery.Mapping;

namespace EFCore.DynamicQuery.Querying;

public static class PropertyTransformQueryExtensions
{
    /// <summary>
    /// Rebuilds every row through a SELECT that applies each mapped property's
    /// <see cref="Mapping.Mutable.MutablePropertyMapping{TDestination, TSource, TProperty}.MutableFunction"/>
    /// (if any) - e.g. unit/currency conversion - and passes every other property through unchanged.
    /// </summary>
    public static IQueryable<TSource> GetTransformedSelect<TSource>(this IQueryable<TSource> query, ITypeMap typeMap)
    {
        var entityType = typeof(TSource);
        var allProperties = entityType.GetProperties();

        var transformedMappings = typeMap.GetPropertyMappings()
            .Where(m => m.MutableFunctionBody is not null)
            .ToList();

        var transformedNames = transformedMappings.Select(m => m.PropertyName).ToList();
        var transformedProperties = allProperties.Where(p => transformedNames.Contains(p.Name, StringComparer.InvariantCultureIgnoreCase));
        var unchangedProperties = allProperties.Except(transformedProperties);

        var parameter = Expression.Parameter(entityType, "x");

        var transformedBindings = transformedProperties.Select(property =>
        {
            var mapping = transformedMappings.First(m => m.PropertyName.Equals(property.Name, StringComparison.InvariantCultureIgnoreCase));
            return CreateTransformedBinding(parameter, property, mapping.MutableFunctionBody!);
        });

        var unchangedBindings = unchangedProperties.Select(property => Expression.Bind(property, Expression.Property(parameter, property.Name)));

        var body = Expression.MemberInit(Expression.New(entityType), unchangedBindings.Concat(transformedBindings));
        var lambda = Expression.Lambda<Func<TSource, TSource>>(body, parameter);

        return query.Select(lambda);
    }

    private static MemberAssignment CreateTransformedBinding(ParameterExpression parameter, PropertyInfo property, Expression mutableFunctionBody)
    {
        var propertyExpression = Expression.Property(parameter, property.Name);
        var transformed = ExpressionParameterHelper.ReplaceRootParameterWithExpression(propertyExpression, mutableFunctionBody);

        return Expression.Bind(property, transformed);
    }
}
