using System.Linq.Expressions;
using EFCore.DynamicQuery.Internal;

namespace EFCore.DynamicQuery.Filtering.Filters;

public sealed class EqualFilter : FilterItem
{
    public object? Value { get; set; }
    public override string FilterKey => "equal";

    public override Expression GetBinaryExpression(Expression property) =>
        LinqExpressionHelper.GetEqualityBinaryExpression(property, DeserializeConstant(Value, property.Type), isEqual: true);
}
