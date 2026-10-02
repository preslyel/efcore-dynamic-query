using EFCore.DynamicQuery.Filtering;
using EFCore.DynamicQuery.Mapping;
using EFCore.DynamicQuery.Querying;
using Microsoft.EntityFrameworkCore;

namespace EFCore.DynamicQuery.SqlServer.Querying;

/// <summary>
/// Combines <see cref="TemporalQueryExtensions"/> with the core <c>Querying</c> pipeline -
/// <see cref="TemporalQueryExtensions.UseHistory{TEntity}"/>/<see cref="TemporalQueryExtensions.UseAllHistory{TEntity}"/>
/// just produce a plain <see cref="IQueryable{T}"/>, so they compose with
/// <c>GetDynamicData</c>/<c>GetDataById</c> for free; these are purely a convenience so callers
/// don't have to chain both calls by hand.
/// </summary>
public static class TemporalDynamicDataExtensions
{
    /// <summary>Runs the full dynamic query pipeline against the entity as it was at <paramref name="asOf"/> (or current data if <c>null</c>).</summary>
    public static Task<QueryDataResultModel<ICollection<TModel>>> GetDynamicDataAsOf<TEntity, TModel>(
        this DbSet<TEntity> source,
        DbContext context,
        DateTime? asOf,
        PageableFilterDataModel filterData,
        IDynamicMapper mapper,
        CancellationToken cancellationToken = default)
        where TEntity : class, new()
        where TModel : class, new() =>
        source.UseHistory(context, asOf).GetDynamicData<TEntity, TModel>(filterData, mapper, cancellationToken);

    /// <summary>Runs the full dynamic query pipeline against every historical version of every row.</summary>
    public static Task<QueryDataResultModel<ICollection<TModel>>> GetAllHistoryDynamicData<TEntity, TModel>(
        this DbSet<TEntity> source,
        DbContext context,
        PageableFilterDataModel filterData,
        IDynamicMapper mapper,
        CancellationToken cancellationToken = default)
        where TEntity : class, new()
        where TModel : class, new() =>
        source.UseAllHistory(context).GetDynamicData<TEntity, TModel>(filterData, mapper, cancellationToken);

    /// <summary>Fetches a single entity by Id as it was at <paramref name="asOf"/> (or current data if <c>null</c>).</summary>
    public static ICollection<TModel> GetDataByIdAsOf<TEntity, TModel, TId>(
        this DbSet<TEntity> source,
        DbContext context,
        DateTime? asOf,
        TId id,
        bool applyTransforms,
        IDynamicMapper mapper)
        where TEntity : class, new()
        where TModel : class, new() =>
        source.UseHistory(context, asOf).GetDataById<TEntity, TModel, TId>(id, applyTransforms, mapper);
}
