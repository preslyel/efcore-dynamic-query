using System.Linq.Expressions;
using EFCore.DynamicQuery.Internal;

namespace EFCore.DynamicQuery.Filtering.Filters;

public sealed class RangeFilter : FilterItem
{
    public required object FromValue { get; set; }
    public required object ToValue { get; set; }
    public override string FilterKey => "range";

    public override Expression GetBinaryExpression(Expression property)
    {
        var fromConstant = DeserializeConstant(FromValue, property.Type);
        var toConstant = DeserializeConstant(ToValue, property.Type);

        return LinqExpressionHelper.GetRangeBinaryExpression(property, fromConstant, toConstant);
    }
}
