using EFCore.DynamicQuery.Filtering;
using EFCore.DynamicQuery.Mapping;

namespace EFCore.DynamicQuery.Querying;

/// <summary>
/// A thin convenience wrapper over <see cref="QueryExtensions"/> that carries its own
/// <see cref="IDynamicMapper"/>, so callers don't have to pass it into every call - construct
/// one per entity type over whatever <see cref="IQueryable{T}"/> you'd otherwise query directly
/// (e.g. <c>context.Set&lt;TEntity&gt;().AsNoTracking()</c>). This class owns no DbContext
/// lifecycle itself - it's a query-building convenience, not a full repository (no CRUD, no
/// transaction or error handling); wrap it in your own repository for those.
/// </summary>
public sealed class DynamicQueryRepository<TEntity>(IQueryable<TEntity> source, IDynamicMapper mapper)
    where TEntity : class, new()
{
    public Task<QueryDataResultModel<ICollection<TModel>>> GetQueryDataResultAsync<TModel>(
        PageableFilterDataModel filterData, CancellationToken cancellationToken = default)
        where TModel : class, new() =>
        source.GetDynamicData<TEntity, TModel>(filterData, mapper, cancellationToken);

    public ICollection<TModel> GetQueryDataResultAsync<TModel>()
        where TModel : class, new() =>
        source.GetDynamicData<TEntity, TModel>(mapper);

    public ICollection<TModel> GetDataById<TModel, TId>(TId id, bool applyTransforms = false)
        where TModel : class, new() =>
        source.GetDataById<TEntity, TModel, TId>(id, applyTransforms, mapper);
}
