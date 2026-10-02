using System.Linq.Expressions;
using EFCore.DynamicQuery.Exceptions;

namespace EFCore.DynamicQuery.Internal;

internal static class LinqExpressionHelper
{
    public static BinaryExpression GetEqualityBinaryExpression(Expression property, ConstantExpression constant, bool isEqual)
    {
        try
        {
            if (Nullable.GetUnderlyingType(property.Type) is not null)
            {
                var hasValueProperty = Expression.Property(property, "HasValue");
                var valueProperty = Expression.Property(property, "Value");

                if (constant.Value is null)
                {
                    return isEqual
                        ? Expression.Equal(hasValueProperty, Expression.Constant(false))
                        : Expression.Equal(hasValueProperty, Expression.Constant(true));
                }

                var targetType = valueProperty.Type;
                var typedConstant = Expression.Constant(Convert.ChangeType(constant.Value, targetType), targetType);

                var equality = isEqual
                    ? Expression.Equal(valueProperty, typedConstant)
                    : Expression.NotEqual(valueProperty, typedConstant);

                return Expression.AndAlso(hasValueProperty, equality);
            }

            var convertedConstant = Expression.Constant(Convert.ChangeType(constant.Value, property.Type), property.Type);

            return isEqual
                ? Expression.Equal(property, convertedConstant)
                : Expression.NotEqual(property, convertedConstant);
        }
        catch (InvalidCastException)
        {
            throw new FilterValueConversionException(property.Type, (property as MemberExpression)?.Member.Name);
        }
    }

    public static Expression GetRangeBinaryExpression(Expression property, ConstantExpression fromConstant, ConstantExpression toConstant)
    {
        var targetType = Nullable.GetUnderlyingType(property.Type) ?? property.Type;

        var fromExpression = Expression.Constant(Convert.ChangeType(fromConstant.Value, targetType), targetType);
        var toExpression = Expression.Constant(Convert.ChangeType(toConstant.Value, targetType), targetType);

        if (targetType == property.Type)
        {
            return Expression.AndAlso(
                Expression.GreaterThanOrEqual(property, fromExpression),
                Expression.LessThanOrEqual(property, toExpression));
        }

        // Nullable property: guard with HasValue before unwrapping .Value, so a null row is
        // excluded from the range instead of throwing when this runs against a literal,
        // per-row evaluation (InMemory/SQLite providers, or a LINQ-to-Objects predicate) rather
        // than a fully declarative SQL translation.
        var hasValueProperty = Expression.Property(property, "HasValue");
        var valueProperty = Expression.Property(property, "Value");

        var rangeCheck = Expression.AndAlso(
            Expression.GreaterThanOrEqual(valueProperty, fromExpression),
            Expression.LessThanOrEqual(valueProperty, toExpression));

        return Expression.AndAlso(hasValueProperty, rangeCheck);
    }
}
