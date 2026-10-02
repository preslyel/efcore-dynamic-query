using System.Reflection;
using EFCore.DynamicQuery.Filtering;
using EFCore.DynamicQuery.Mapping;

namespace EFCore.DynamicQuery.Querying;

/// <summary>
/// Resolves <typeparamref name="TModel"/>'s registered source (entity) type via
/// <see cref="IDynamicMapper.GetSourceType{TDestination}"/>, gets a queryable for it from
/// <see cref="IQueryableProvider"/>, and dispatches into <see cref="QueryExtensions"/> reflectively -
/// TEntity is only known at runtime here, never as a compile-time generic parameter, which is
/// exactly what lets a caller write <c>GetQueryDataResultAsync&lt;SomeModel&gt;(filter, ct)</c>
/// without a repository/service written specifically for SomeModel.
/// </summary>
public sealed class DynamicQueryService(IQueryableProvider queryableProvider, IDynamicMapper mapper) : IDynamicQueryService
{
    // Resolved once per process rather than re-scanned via GetMethods().First(...) on every call -
    // this service is DI-registered Scoped, so it's re-instantiated (and these would otherwise be
    // re-resolved) on every request.
    private static readonly MethodInfo GetDynamicDataAsyncMethod =
        FindGetDynamicDataMethod(parameterCount: 4, filterParameterType: typeof(PageableFilterDataModel));

    private static readonly MethodInfo GetDynamicDataMethod =
        FindGetDynamicDataMethod(parameterCount: 2, filterParameterType: null);

    private static readonly MethodInfo GetDataByIdMethod =
        typeof(QueryExtensions).GetMethod(nameof(QueryExtensions.GetDataById))!;

    private static readonly MethodInfo GetQueryableMethod =
        typeof(IQueryableProvider).GetMethod(nameof(IQueryableProvider.GetQueryable))!;

    public Task<QueryDataResultModel<ICollection<TModel>>> GetQueryDataResultAsync<TModel>(
        PageableFilterDataModel filterData, CancellationToken cancellationToken = default)
        where TModel : class, new()
    {
        var entityType = mapper.GetSourceType<TModel>();
        var query = GetQueryable(entityType);

        var method = GetDynamicDataAsyncMethod.MakeGenericMethod(entityType, typeof(TModel));

        return (Task<QueryDataResultModel<ICollection<TModel>>>)method.Invoke(null, [query, filterData, mapper, cancellationToken])!;
    }

    public ICollection<TModel> GetQueryDataResultAsync<TModel>()
        where TModel : class, new()
    {
        var entityType = mapper.GetSourceType<TModel>();
        var query = GetQueryable(entityType);

        var method = GetDynamicDataMethod.MakeGenericMethod(entityType, typeof(TModel));

        return (ICollection<TModel>)method.Invoke(null, [query, mapper])!;
    }

    public ICollection<TModel> GetDataById<TModel, TId>(TId id, bool applyTransforms = false)
        where TModel : class, new()
    {
        var entityType = mapper.GetSourceType<TModel>();
        var query = GetQueryable(entityType);

        var method = GetDataByIdMethod.MakeGenericMethod(entityType, typeof(TModel), typeof(TId));

        return (ICollection<TModel>)method.Invoke(null, [query, id, applyTransforms, mapper])!;
    }

    private object GetQueryable(Type entityType)
    {
        var method = GetQueryableMethod.MakeGenericMethod(entityType);

        return method.Invoke(queryableProvider, null)!;
    }

    private static MethodInfo FindGetDynamicDataMethod(int parameterCount, Type? filterParameterType) =>
        typeof(QueryExtensions)
            .GetMethods()
            .First(m => m.Name == nameof(QueryExtensions.GetDynamicData)
                && m.GetParameters().Length == parameterCount
                && (filterParameterType is null || m.GetParameters()[1].ParameterType == filterParameterType));
}
