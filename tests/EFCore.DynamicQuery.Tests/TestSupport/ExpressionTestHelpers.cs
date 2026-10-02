using System.Linq.Expressions;

namespace EFCore.DynamicQuery.Tests.TestSupport;

internal static class ExpressionTestHelpers
{
    /// <summary>
    /// Compiles and invokes a <see cref="Mapping.PropertyMapping.PropertyBody"/> expression
    /// against a real <paramref name="sourceInstance"/> - the same thing a consumer's compiled
    /// mapping delegate does internally, used here to assert a raw <c>ITypeMap</c> registration
    /// resolves to the right value without going through <c>DynamicMapper</c>.
    /// </summary>
    public static object? Evaluate<TSource>(Expression propertyBody, TSource sourceInstance)
    {
        var parameter = FindParameter(propertyBody, typeof(TSource))
            ?? throw new InvalidOperationException($"No parameter of type {typeof(TSource)} found in the expression.");

        var lambda = Expression.Lambda(propertyBody, parameter);
        return lambda.Compile().DynamicInvoke(sourceInstance);
    }

    private static ParameterExpression? FindParameter(Expression expression, Type parameterType)
    {
        ParameterExpression? found = null;

        void Visit(Expression? node)
        {
            if (node is null || found is not null) return;
            if (node is ParameterExpression p && p.Type == parameterType) { found = p; return; }
            if (node is MemberExpression m) Visit(m.Expression);
        }

        Visit(expression);
        return found;
    }
}
