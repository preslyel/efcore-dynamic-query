using System.Linq.Expressions;

namespace EFCore.DynamicQuery.Internal;

/// <summary>
/// Rewrites every occurrence of <paramref name="oldRootParam"/> in a visited expression to
/// <paramref name="newRootParam"/>, skipping any occurrence that's shadowed by a nested lambda's
/// own parameter of the same identity. Needed whenever a stored expression (built once, against
/// its own parameter) has to be spliced into a freshly built expression tree that uses a
/// different parameter instance for the same logical value.
/// </summary>
internal sealed class SafeRootParameterReplacer(ParameterExpression oldRootParam, ParameterExpression newRootParam) : ExpressionVisitor
{
    private readonly HashSet<ParameterExpression> scoped = [];

    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        foreach (var param in node.Parameters)
            scoped.Add(param);

        var body = Visit(node.Body);

        foreach (var param in node.Parameters)
            scoped.Remove(param);

        return node.Update(body, node.Parameters);
    }

    protected override Expression VisitParameter(ParameterExpression node) =>
        node == oldRootParam && !scoped.Contains(node) ? newRootParam : base.VisitParameter(node);
}
