using System.Linq.Expressions;
using System.Text.Json;

namespace EFCore.DynamicQuery.Filtering;

/// <summary>
/// Base class for a custom filter: override <see cref="FilterKey"/> with a unique wire name and
/// <see cref="GetBinaryExpression"/> with the comparison logic, then register it -
/// <c>registry.Register&lt;MyFilter&gt;("myFilter")</c> - so <see cref="FilterJsonConverter"/>
/// can resolve it from an incoming <c>{"filter": "myFilter", ...}</c> payload.
/// </summary>
public abstract class FilterItem : IFilterItem
{
    public abstract string FilterKey { get; }
    public required string Name { get; set; }

    public abstract Expression GetBinaryExpression(Expression property);

    /// <summary>
    /// Converts a raw filter value into a constant of the filtered property's own type. Handles
    /// both ways a value reaches a filter: as a boxed <see cref="JsonElement"/> (any <c>object</c>-typed
    /// value property deserialized by <see cref="FilterJsonConverter"/>) and as an already-typed
    /// CLR value (a filter built directly in code, with no JSON involved at all).
    /// </summary>
    protected static ConstantExpression DeserializeConstant(object? value, Type targetType) =>
        Expression.Constant(ConvertValue(value, targetType), targetType);

    /// <inheritdoc cref="DeserializeConstant"/>
    /// <remarks>Same JsonElement-vs-plain-value conversion, for a caller that needs the raw value rather than a wrapping <see cref="ConstantExpression"/> - e.g. building a typed <c>List&lt;T&gt;</c> for a <c>Contains</c> check.</remarks>
    protected static object? ConvertValue(object? value, Type targetType) => value switch
    {
        null => null,
        JsonElement json => JsonSerializer.Deserialize(json, targetType),
        _ => Convert.ChangeType(value, targetType)
    };
}
