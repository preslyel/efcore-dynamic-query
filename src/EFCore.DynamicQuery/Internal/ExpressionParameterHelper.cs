using System.Linq.Expressions;

namespace EFCore.DynamicQuery.Internal;

internal static class ExpressionParameterHelper
{
    /// <summary>
    /// Rebinds a stored property expression (built against its own parameter, e.g. from
    /// <see cref="Mapping.PropertyMapping.PropertyBody"/>) onto <paramref name="newParameter"/>,
    /// so it can be spliced into a lambda that uses that parameter instead.
    /// </summary>
    public static Expression ReplaceRootParameter(ParameterExpression newParameter, Expression expression) =>
        new SafeRootParameterReplacer(FindRootParameter(expression), newParameter).Visit(expression);

    /// <summary>
    /// Same as <see cref="ReplaceRootParameter"/>, but the replacement can be any expression -
    /// e.g. splicing a single-argument function body onto a real member access instead of
    /// just another parameter.
    /// </summary>
    public static Expression ReplaceRootParameterWithExpression(Expression replacement, Expression expression) =>
        new ParameterToExpressionReplacer(FindRootParameter(expression), replacement).Visit(expression);

    private static ParameterExpression FindRootParameter(Expression expression)
    {
        var collector = new ParameterReplacerVisitor();
        collector.Visit(expression);

        return collector.Parameters.FirstOrDefault()
            ?? throw new InvalidOperationException("No parameter found in the expression.");
    }
}
