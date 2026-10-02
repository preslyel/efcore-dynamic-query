using System.Linq.Expressions;

namespace EFCore.DynamicQuery.Filtering.Filters;

public sealed class InFilter : FilterItem
{
    public IEnumerable<object> Values { get; set; } = [];
    public override string FilterKey => "in";

    public override Expression GetBinaryExpression(Expression property)
    {
        if (!Values.Any())
            return Expression.Equal(Expression.Constant(false), Expression.Constant(true));

        var propertyType = property.Type;
        var elementType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        var nonNullValues = Values
            .Where(v => v is not null)
            .Select(v => ConvertToType(v, elementType))
            .ToList();

        var listType = typeof(List<>).MakeGenericType(elementType);
        var castMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.Cast))!.MakeGenericMethod(elementType);
        var typedEnumerable = castMethod.Invoke(null, [nonNullValues])!;
        var typedList = Activator.CreateInstance(listType, typedEnumerable)!;
        var containsMethod = listType.GetMethod("Contains", [elementType])!;

        var isNullable = Nullable.GetUnderlyingType(propertyType) is not null;
        Expression propertyToCheck = isNullable ? Expression.Property(property, "Value") : property;

        Expression containsCheck = Expression.Call(Expression.Constant(typedList), containsMethod, propertyToCheck);

        if (!isNullable)
            return containsCheck;

        // Guard with HasValue before unwrapping .Value, so a null row is excluded from the set
        // instead of throwing when this runs against a literal, per-row evaluation (InMemory/SQLite
        // providers, or a LINQ-to-Objects predicate) rather than a fully declarative SQL translation.
        var hasValueProperty = Expression.Property(property, "HasValue");
        return Expression.AndAlso(hasValueProperty, containsCheck);
    }

    private static object ConvertToType(object value, Type targetType) => ConvertValue(value, targetType)!;
}
