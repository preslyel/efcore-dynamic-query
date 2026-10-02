using System.Linq.Expressions;
using EFCore.DynamicQuery.Exceptions;
using EFCore.DynamicQuery.Filtering;
using EFCore.DynamicQuery.Internal;
using EFCore.DynamicQuery.Mapping;
using Microsoft.EntityFrameworkCore;

namespace EFCore.DynamicQuery.Querying;

/// <summary>
/// The main entry points tying every other piece in <c>Querying</c> together: filter, order,
/// page, include, and (optionally) group or transform a query, then map it to
/// <typeparamref name="TModel"/> in one call.
/// </summary>
public static class QueryExtensions
{
    public static async Task<QueryDataResultModel<ICollection<TModel>>> GetDynamicData<TEntity, TModel>(
        this IQueryable<TEntity> query, PageableFilterDataModel filterData, IDynamicMapper mapper, CancellationToken cancellationToken = default)
        where TEntity : class, new()
        where TModel : class, new()
    {
        var typeMap = mapper.GetTypeMap(typeof(TEntity), typeof(TModel));

        if (filterData.Filters is { Length: > 0 })
            query = query.GetFilteredData(filterData.Filters, typeMap, filterData.ApplyTransforms);

        return string.IsNullOrEmpty(filterData.GroupBy)
            ? await GetUngroupedResult<TEntity, TModel>(query, filterData, mapper, typeMap, cancellationToken)
            : await GetGroupedResult<TEntity, TModel>(query, filterData, typeMap, cancellationToken);
    }

    private static async Task<QueryDataResultModel<ICollection<TModel>>> GetUngroupedResult<TEntity, TModel>(
        IQueryable<TEntity> query, PageableFilterDataModel filterData, IDynamicMapper mapper, ITypeMap typeMap, CancellationToken cancellationToken)
        where TEntity : class, new()
        where TModel : class, new()
    {
        var count = await query.CountAsync(cancellationToken);

        query = query.GetOrderedData(filterData.OrderBy, filterData.Ascending, typeMap, filterData.ApplyTransforms);
        query = query.GetPagedData(filterData);

        var headers = GetHeaders(filterData.Fields, typeMap, typeof(TModel));
        query = query.IncludeEntities(headers, typeMap);

        if (filterData.ApplyTransforms)
            query = query.GetTransformedSelect(typeMap);

        var collection = mapper.Map<TEntity, TModel>(query.AsEnumerable(), headers);

        return new QueryDataResultModel<ICollection<TModel>> { Result = collection, TotalCount = count };
    }

    private static async Task<QueryDataResultModel<ICollection<TModel>>> GetGroupedResult<TEntity, TModel>(
        IQueryable<TEntity> query, PageableFilterDataModel filterData, ITypeMap typeMap, CancellationToken cancellationToken)
        where TEntity : class, new()
        where TModel : class, new()
    {
        if (filterData.ApplyTransforms)
            query = query.GetTransformedSelect(typeMap);

        // GetGroupedValues intentionally does NOT re-apply MutableFunction here - the transform
        // (if requested) already ran above via GetTransformedSelect, so the entity's own property
        // values are already in "what the client sees" space by this point; applying it again
        // would double-transform.
        var groupedQuery = query.GetGroupedValues(filterData.GroupBy, typeMap);
        var groupedCount = await groupedQuery.CountAsync(cancellationToken);

        if (filterData.GroupBy.Equals(filterData.OrderBy, StringComparison.Ordinal))
            groupedQuery = filterData.Ascending ? groupedQuery.OrderBy(x => x) : groupedQuery.OrderByDescending(x => x);

        groupedQuery = groupedQuery.GetPagedData(filterData);
        var groupedResult = await groupedQuery.ToListAsync(cancellationToken);

        var createModel = GetGroupedResultCreateDelegate<TModel>(typeMap, filterData.GroupBy);
        var groupedCollection = groupedResult.Select(createModel).ToList();

        return new QueryDataResultModel<ICollection<TModel>> { Result = groupedCollection, TotalCount = groupedCount };
    }

    public static ICollection<TModel> GetDynamicData<TEntity, TModel>(this IQueryable<TEntity> query, IDynamicMapper mapper)
        where TEntity : class, new()
        where TModel : class, new()
    {
        var typeMap = mapper.GetTypeMap(typeof(TEntity), typeof(TModel));
        var headers = GetHeaders(null, typeMap, typeof(TModel));

        query = query.IncludeEntities(headers, typeMap);

        return mapper.Map<TEntity, TModel>(query.AsEnumerable(), headers);
    }

    /// <summary>
    /// Fetches a single entity by its <c>Id</c> property (assumed present on every
    /// <typeparamref name="TEntity"/>, of type <typeparamref name="TId"/> - int, long, Guid,
    /// whatever the real model declares) and maps it to <typeparamref name="TModel"/>. Reads
    /// the property through <see cref="EF.Property{TProperty}"/> rather than a plain member
    /// access, so this also works for a shadow "Id" property with no real CLR member.
    /// </summary>
    public static ICollection<TModel> GetDataById<TEntity, TModel, TId>(
        this IQueryable<TEntity> query, TId id, bool applyTransforms, IDynamicMapper mapper)
        where TEntity : class, new()
        where TModel : class, new()
    {
        var typeMap = mapper.GetTypeMap(typeof(TEntity), typeof(TModel));
        var headers = GetHeaders([], typeMap, typeof(TModel));

        var parameter = Expression.Parameter(typeof(TEntity), "e");
        var propertyCall = Expression.Call(
            typeof(EF).GetMethod(nameof(EF.Property))!.MakeGenericMethod(typeof(TId)), parameter, Expression.Constant("Id"));
        var predicate = Expression.Lambda<Func<TEntity, bool>>(
            Expression.Equal(propertyCall, Expression.Constant(id, typeof(TId))), parameter);

        query = query.Where(predicate);
        query = query.IncludeEntities(headers, typeMap);

        if (applyTransforms)
            query = query.GetTransformedSelect(typeMap);

        return mapper.Map<TEntity, TModel>(query.AsEnumerable(), headers);
    }

    private static string[] GetHeaders(List<string>? fields, ITypeMap typeMap, Type destinationType)
    {
        if (fields is null || fields.Count == 0)
            return [.. destinationType.GetProperties().Select(p => p.Name)];

        var headers = new List<string>(fields);

        foreach (var requiredField in typeMap.GetRequiredFields())
        {
            if (!headers.Contains(requiredField, StringComparer.OrdinalIgnoreCase))
                headers.Add(requiredField);
        }

        return [.. headers];
    }

    private static IQueryable<object> GetGroupedValues<TEntity>(this IQueryable<TEntity> query, string groupBy, ITypeMap typeMap)
    {
        var sourceProperty = typeMap.GetSourceProperty(groupBy) ?? throw new PropertyNotMappedException(groupBy);
        var parameter = Expression.Parameter(typeof(TEntity), "x");
        var memberExpression = ExpressionParameterHelper.ReplaceRootParameter(parameter, sourceProperty);

        var lambda = Expression.Lambda<Func<TEntity, object>>(Expression.Convert(memberExpression, typeof(object)), parameter);
        return query.Select(lambda).Distinct();
    }

    private static Func<object, TModel> GetGroupedResultCreateDelegate<TModel>(ITypeMap typeMap, string propertyName)
        where TModel : class, new()
    {
        var propertyMapping = typeMap.GetPropertyMapping(propertyName) ?? throw new PropertyNotMappedException(propertyName);

        var parameter = Expression.Parameter(typeof(object), "value");
        var convertExpression = Expression.Convert(parameter, propertyMapping.PropertyType);
        var bindExpression = Expression.Bind(propertyMapping.PropertyMemberInfo, convertExpression);

        var newExpression = Expression.New(typeof(TModel));
        var memberInitExpression = Expression.MemberInit(newExpression, bindExpression);

        return Expression.Lambda<Func<object, TModel>>(memberInitExpression, parameter).Compile();
    }
}
