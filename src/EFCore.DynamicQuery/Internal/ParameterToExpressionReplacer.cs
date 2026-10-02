using System.Linq.Expressions;

namespace EFCore.DynamicQuery.Internal;

/// <summary>
/// Like <see cref="SafeRootParameterReplacer"/>, but the replacement doesn't have to be another
/// parameter - e.g. splicing a stored single-argument function body (whose own parameter stands
/// for "the property value") onto a real member access such as <c>x.SomeProperty</c>.
/// </summary>
internal sealed class ParameterToExpressionReplacer(ParameterExpression parameter, Expression replacement) : ExpressionVisitor
{
    protected override Expression VisitParameter(ParameterExpression node) =>
        node == parameter ? replacement : base.VisitParameter(node);
}
