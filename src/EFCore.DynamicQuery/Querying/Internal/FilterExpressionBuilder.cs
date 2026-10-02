using System.Linq.Expressions;
using System.Reflection;
using EFCore.DynamicQuery.Filtering;
using EFCore.DynamicQuery.Internal;

namespace EFCore.DynamicQuery.Querying.Internal;

/// <summary>
/// Builds filter/order expressions against navigation-collection-valued mapped properties -
/// e.g. filtering an entity by a condition on one of its child collection's properties (an
/// "Any" predicate), or unrolling a Select/Where chain to find the real collection root.
/// </summary>
internal static class FilterExpressionBuilder
{
    // Resolved once per process rather than re-scanned via GetMethods() on every BuildAnyPredicate call.
    private static readonly MethodInfo AnyMethod = typeof(Enumerable)
        .GetMethods()
        .First(m => m.Name == nameof(Enumerable.Any) && m.GetParameters().Length == 2);

    internal sealed class Chain
    {
        public Expression Root { get; set; } = null!;
        public List<LambdaExpression> Selectors { get; } = [];
        public List<LambdaExpression> Filters { get; } = [];
    }

    public static Chain UnrollChain(Expression expression)
    {
        var chain = new Chain();

        while (true)
        {
            if (expression is MethodCallExpression call)
            {
                if (call.Method.Name is nameof(Enumerable.Select) or nameof(Enumerable.SelectMany))
                {
                    chain.Selectors.Insert(0, (LambdaExpression)StripQuotes(call.Arguments[1]));
                    expression = call.Arguments[0];
                }
                else if (call.Method.Name == nameof(Enumerable.Where))
                {
                    chain.Filters.Insert(0, (LambdaExpression)StripQuotes(call.Arguments[1]));
                    expression = call.Arguments[0];
                }
                else if (call.Method.Name == nameof(string.Join) && call.Method.DeclaringType == typeof(string) && call.Arguments.Count >= 2)
                {
                    expression = call.Arguments[1];
                }
                else if (call.Method.Name == nameof(Enumerable.Distinct))
                {
                    expression = call.Arguments[0];
                }
                else
                {
                    break;
                }
            }
            else if (expression is ConditionalExpression conditional)
            {
                expression = conditional.IfTrue is MethodCallExpression ? conditional.IfTrue : conditional.IfFalse;
            }
            else
            {
                break;
            }
        }

        chain.Root = expression;
        return chain;
    }

    private static Expression StripQuotes(Expression expression)
    {
        while (expression.NodeType == ExpressionType.Quote)
            expression = ((UnaryExpression)expression).Operand;

        return expression;
    }

    public static Expression<Func<T, bool>> BuildAnyPredicate<T>(
        Expression selectorExpression, IFilterItem filter, ParameterExpression rootParameter)
    {
        var chain = UnrollChain(selectorExpression);

        var navRoot = new SafeRootParameterReplacer(rootParameter, rootParameter).Visit(chain.Root);

        var elementType = navRoot.Type.GetGenericArguments()[0];
        var elementParameter = Expression.Parameter(elementType, "e");

        Expression currentNav = elementParameter;

        foreach (var selector in chain.Selectors)
            currentNav = new SafeRootParameterReplacer(selector.Parameters[0], elementParameter).Visit(selector.Body);

        Expression predicateBody = filter.GetBinaryExpression(currentNav);

        foreach (var chainFilter in chain.Filters.AsEnumerable().Reverse())
        {
            var replacedFilter = new SafeRootParameterReplacer(chainFilter.Parameters[0], elementParameter).Visit(chainFilter.Body);
            predicateBody = Expression.AndAlso(replacedFilter, predicateBody);
        }

        var anyLambda = Expression.Lambda(predicateBody, elementParameter);

        var anyCall = Expression.Call(AnyMethod.MakeGenericMethod(elementType), navRoot, anyLambda);

        return Expression.Lambda<Func<T, bool>>(anyCall, rootParameter);
    }

    public static bool IsCollectionExpression(Expression expression)
    {
        expression = StripQuotes(expression);

        while (expression is ConditionalExpression conditional)
            expression = conditional.IfTrue is MethodCallExpression ? conditional.IfTrue : conditional.IfFalse;

        if (expression is not MethodCallExpression methodCall)
            return false;

        if (methodCall.Method.Name == nameof(string.Join) && methodCall.Method.DeclaringType == typeof(string) && methodCall.Arguments.Count >= 2)
        {
            var joinEnumerableType = methodCall.Arguments[1].Type;
            return typeof(System.Collections.IEnumerable).IsAssignableFrom(joinEnumerableType) && joinEnumerableType != typeof(string);
        }

        var returnType = methodCall.Type;
        return typeof(System.Collections.IEnumerable).IsAssignableFrom(returnType) && returnType != typeof(string);
    }

    public static bool IsSimplePropertyJoin(MethodCallExpression methodCall)
    {
        if (methodCall.Method.Name != nameof(string.Join) || methodCall.Method.DeclaringType != typeof(string))
            return false;

        var collectionArgument = StripQuotes(methodCall.Arguments[1]);

        return collectionArgument is NewExpression or ListInitExpression or NewArrayExpression;
    }

    public static List<Expression> ExtractProperties(Expression expression)
    {
        if (expression is LambdaExpression lambda)
            expression = lambda.Body;

        if (expression is MethodCallExpression methodCall)
        {
            var listArgument = methodCall.Arguments.FirstOrDefault(x => x is NewExpression or ListInitExpression);

            if (listArgument is NewExpression newExpression)
                return [.. newExpression.Arguments];

            if (listArgument is ListInitExpression listInit)
                return [.. listInit.Initializers.SelectMany(i => i.Arguments)];
        }

        throw new NotSupportedException("Unsupported expression format.");
    }

    public static string ExtractSeparator(MethodCallExpression methodCall)
    {
        var separatorExpression = StripQuotes(methodCall.Arguments[0]);

        if (separatorExpression is ConstantExpression constant)
            return constant.Value?.ToString() ?? string.Empty;

        throw new NotSupportedException("Only constant separators are supported.");
    }
}
