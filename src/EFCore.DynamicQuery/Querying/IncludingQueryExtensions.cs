using System.Linq.Expressions;
using System.Reflection;
using EFCore.DynamicQuery.Exceptions;
using EFCore.DynamicQuery.Mapping;
using EFCore.DynamicQuery.Querying.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace EFCore.DynamicQuery.Querying;

public static class IncludingQueryExtensions
{
    // Resolved once per process rather than re-scanned via GetMethods() on every Include/ThenInclude
    // call - these overload sets never change at runtime.
    private static readonly MethodInfo IncludeMethod = typeof(EntityFrameworkQueryableExtensions)
        .GetMethods()
        .First(m => m.Name == nameof(EntityFrameworkQueryableExtensions.Include) && m.GetParameters().Length == 2);

    private static readonly MethodInfo ThenIncludeFromEnumerableMethod = FindThenIncludeMethod(isParentEnumerable: true);
    private static readonly MethodInfo ThenIncludeFromReferenceMethod = FindThenIncludeMethod(isParentEnumerable: false);

    /// <summary>
    /// Applies <c>.Include()</c>/<c>.ThenInclude()</c> for every navigation path each of
    /// <paramref name="fields"/>'s mapped source expression touches.
    /// </summary>
    public static IQueryable<TEntity> IncludeEntities<TEntity>(this IQueryable<TEntity> query, IEnumerable<string> fields, ITypeMap typeMap)
        where TEntity : class
    {
        foreach (var field in fields)
        {
            var expression = typeMap.GetSourceProperty(field) ?? throw new PropertyNotMappedException(field);
            var includePaths = new CustomExpressionVisitor(typeof(TEntity)).Extract(expression);

            query = (IQueryable<TEntity>)ApplyIncludePaths(query, typeof(TEntity), includePaths);
        }

        return query;
    }

    /// <inheritdoc cref="IncludeEntities{TEntity}(IQueryable{TEntity}, IEnumerable{string}, ITypeMap)"/>
    public static IQueryable<object> IncludeEntities(this IQueryable<object> query, Type entityType, IEnumerable<string> fields, ITypeMap typeMap)
    {
        foreach (var field in fields)
        {
            var expression = typeMap.GetSourceProperty(field) ?? throw new PropertyNotMappedException(field);
            var includePaths = new CustomExpressionVisitor(entityType).Extract(expression);

            query = (IQueryable<object>)ApplyIncludePaths(query, entityType, includePaths);
        }

        return query;
    }

    // Include/ThenInclude are only ever invoked reflectively here (MakeGenericMethod + Invoke)
    // regardless of the caller's static queryable type, so one implementation serves both the
    // generic <TEntity> and the Type-driven overloads above - the original ported code had two
    // near-identical copies of everything below, one per overload.
    private static object ApplyIncludePaths(object query, Type entityType, IEnumerable<string> includePaths)
    {
        foreach (var includePath in includePaths)
        {
            var propertyParts = includePath.Split('.');
            var property = entityType.GetProperty(propertyParts[0], BindingFlags.Public | BindingFlags.Instance);

            if (property is null) continue;

            query = ApplyInclude(query, entityType, propertyParts[0]);

            var currentType = property.PropertyType;
            var currentProperty = property;

            for (var i = 1; i < propertyParts.Length; i++)
            {
                var isEnumerable = typeof(IEnumerable<>).IsAssignableFromGeneric(currentProperty.PropertyType);
                currentType = isEnumerable
                    ? currentProperty.PropertyType.GetGenericArguments().FirstOrDefault() ?? typeof(object)
                    : currentProperty.PropertyType;

                query = ApplyThenInclude(query, entityType, propertyParts[i], currentType, isEnumerable);
                currentProperty = currentType.GetProperty(propertyParts[i], BindingFlags.Public | BindingFlags.Instance)!;
            }
        }

        return query;
    }

    private static object ApplyInclude(object query, Type entityType, string navigationProperty)
    {
        var parameter = Expression.Parameter(entityType, "x");
        var propertyAccess = Expression.PropertyOrField(parameter, navigationProperty);
        var lambda = Expression.Lambda(propertyAccess, parameter);

        var genericIncludeMethod = IncludeMethod.MakeGenericMethod(entityType, propertyAccess.Type);

        return genericIncludeMethod.Invoke(null, [query, lambda])!;
    }

    private static object ApplyThenInclude(object query, Type entityType, string navigationProperty, Type parentPropertyType, bool isEnumerable)
    {
        var method = isEnumerable ? ThenIncludeFromEnumerableMethod : ThenIncludeFromReferenceMethod;

        var parameter = Expression.Parameter(parentPropertyType, "x");
        var propertyAccess = Expression.PropertyOrField(parameter, navigationProperty);
        var lambda = Expression.Lambda(propertyAccess, parameter);

        var genericMethod = method.MakeGenericMethod(entityType, parentPropertyType, propertyAccess.Type);

        return genericMethod.Invoke(null, [query, lambda])!;
    }

    private static MethodInfo FindThenIncludeMethod(bool isParentEnumerable)
    {
        var thenIncludeMethods = typeof(EntityFrameworkQueryableExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == nameof(EntityFrameworkQueryableExtensions.ThenInclude) && m.GetParameters().Length == 2);

        foreach (var method in thenIncludeMethods)
        {
            var firstParameterType = method.GetParameters()[0].ParameterType;
            if (!firstParameterType.IsGenericType || firstParameterType.GetGenericTypeDefinition() != typeof(IIncludableQueryable<,>))
                continue;

            var previousPropertyType = firstParameterType.GetGenericArguments()[1];
            var isEnumerable = previousPropertyType.IsGenericType && previousPropertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>);

            if (isEnumerable == isParentEnumerable)
                return method;
        }

        throw new InvalidOperationException($"No matching ThenInclude overload found for isParentEnumerable={isParentEnumerable}.");
    }
}
