using System.Linq.Expressions;
using System.Reflection;
using EFCore.DynamicQuery.Internal;

namespace EFCore.DynamicQuery.Filtering.Filters;

/// <summary>
/// ANDs an equality check across several sibling properties of the same object -
/// e.g. matching a composite key - rather than filtering a single named property.
/// </summary>
public sealed class NamedEqualFilter : FilterItem
{
    public required IEnumerable<NameValue> NameValues { get; set; }
    public override string FilterKey => "namedEqual";

    public override Expression GetBinaryExpression(Expression property)
    {
        if (!NameValues.Any())
            return Expression.Equal(Expression.Constant(false), Expression.Constant(true));

        Expression? combined = null;

        foreach (var item in NameValues)
        {
            var itemProperty = Expression.Property(property, item.Name);
            var itemPropertyType = ((PropertyInfo)itemProperty.Member).PropertyType;
            var equality = LinqExpressionHelper.GetEqualityBinaryExpression(
                itemProperty, DeserializeConstant(item.Value, itemPropertyType), isEqual: true);

            combined = combined is null ? equality : Expression.AndAlso(combined, equality);
        }

        return Expression.Equal(combined!, Expression.Constant(true));
    }
}
