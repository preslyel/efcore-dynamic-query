using System.Linq.Expressions;
using EFCore.DynamicQuery.Internal;
using EFCore.DynamicQuery.Mapping;
using EFCore.DynamicQuery.Mapping.Mutable;

namespace EFCore.DynamicQuery.Querying.Internal;

internal static class TypeMapExtensions
{
    /// <summary>
    /// Resolves the source-side expression for a destination property name - applying its
    /// <see cref="MutablePropertyMapping{TDestination, TSource, TProperty}.MutableFunction"/>
    /// (if any and if <paramref name="applyTransforms"/>) so a filter/order/group-by name
    /// compares against what the client sees (the transformed value), not the raw stored one.
    /// A client working against transformed results has no way to know the raw stored value, so
    /// this only makes sense gated on the same flag controlling whether results are transformed.
    /// </summary>
    public static Expression? GetEffectiveSourceProperty(this ITypeMap typeMap, string propertyName, bool applyTransforms)
    {
        var propertyMapping = typeMap.GetPropertyMapping(propertyName);
        if (propertyMapping is null)
            return null;

        if (!applyTransforms)
            return propertyMapping.PropertyBody;

        var mutableFunctionBody = propertyMapping.MutableFunctionBody;

        return mutableFunctionBody is null
            ? propertyMapping.PropertyBody
            : ExpressionParameterHelper.ReplaceRootParameterWithExpression(propertyMapping.PropertyBody, mutableFunctionBody);
    }
}
