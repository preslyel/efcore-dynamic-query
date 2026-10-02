using System.Linq.Expressions;
using System.Reflection;

namespace EFCore.DynamicQuery.Querying.Internal;

/// <summary>
/// Walks a mapped property expression and extracts every navigation/collection path it touches
/// (e.g. "Order.Customer", "Lines") - the set of <c>.Include()</c>/<c>.ThenInclude()</c> paths
/// needed for that expression to evaluate without a null-navigation surprise.
/// </summary>
internal sealed class CustomExpressionVisitor(Type entityType) : ExpressionVisitor
{
    private readonly Dictionary<ParameterExpression, string> parameterScopes = [];
    private readonly HashSet<string> paths = [];

    public List<string> Extract(Expression expression)
    {
        paths.Clear();
        parameterScopes.Clear();
        Visit(expression);

        return PrunePrefixes(paths);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        foreach (var arg in node.Arguments)
        {
            if (arg is UnaryExpression { Operand: LambdaExpression lambdaUnary })
            {
                TrackLambdaParameter(lambdaUnary, node);
                Visit(lambdaUnary.Body);
            }
            else if (arg is LambdaExpression lambda)
            {
                TrackLambdaParameter(lambda, node);
                Visit(lambda.Body);
            }
            else
            {
                Visit(arg);
            }
        }

        if (node.Object is not null)
            Visit(node.Object);

        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        var fullPath = ResolveFullPath(node);

        if (!string.IsNullOrEmpty(fullPath) && IsNavigationProperty(node.Member))
            paths.Add(fullPath);

        return base.VisitMember(node);
    }

    private void TrackLambdaParameter(LambdaExpression lambda, MethodCallExpression method)
    {
        if (lambda.Parameters.Count != 1)
            return;

        var basePath = method.Object is not null
            ? ResolveFullPathFromExpression(method.Object)
            : method.Arguments.Count > 0 ? ResolveFullPathFromExpression(method.Arguments[0]) : null;

        if (!string.IsNullOrEmpty(basePath))
            parameterScopes[lambda.Parameters[0]] = basePath;
    }

    private string? ResolveFullPathFromExpression(Expression? expression) => expression switch
    {
        MemberExpression member => ResolveFullPath(member),
        MethodCallExpression method => ResolveFullPathFromExpression(method.Object) ?? ResolveFullPathFromExpression(method.Arguments.FirstOrDefault()),
        ParameterExpression parameter => parameterScopes.TryGetValue(parameter, out var root) ? root : null,
        UnaryExpression unary => ResolveFullPathFromExpression(unary.Operand),
        _ => null
    };

    private string? ResolveFullPath(MemberExpression member)
    {
        var segments = new List<string>();
        Expression? current = member;

        while (current is MemberExpression memberExpression)
        {
            segments.Insert(0, memberExpression.Member.Name);
            current = memberExpression.Expression;
        }

        while (current is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
            current = unary.Operand;

        switch (current)
        {
            case ParameterExpression parameter when parameterScopes.TryGetValue(parameter, out var root):
                segments.Insert(0, root);
                break;
            case ParameterExpression parameter when parameter.Type != entityType:
                return null;
            case not null and not ParameterExpression:
                return null;
        }

        return string.Join(".", segments);
    }

    private static List<string> PrunePrefixes(HashSet<string> paths)
    {
        var result = new HashSet<string>(paths);

        foreach (var path in paths)
        {
            if (paths.Any(other => other != path && other.StartsWith(path + ".", StringComparison.Ordinal)))
                result.Remove(path);
        }

        return [.. result];
    }

    private static bool IsNavigationProperty(MemberInfo memberInfo)
    {
        var memberType = memberInfo.DeclaringType?.GetProperty(memberInfo.Name)?.PropertyType;

        return memberType is not null && !memberType.IsValueType && memberType != typeof(string);
    }
}
