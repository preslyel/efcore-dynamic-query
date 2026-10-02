namespace EFCore.DynamicQuery.Querying;

/// <summary>
/// Supplies an <see cref="IQueryable{T}"/> for an entity type known only at runtime - what
/// <see cref="DynamicQueryService"/> needs to go from "just a model type" to a real query, without
/// this library dictating how that query is built (tracking, global filters, multi-tenancy, etc.
/// are entirely up to your implementation).
/// </summary>
public interface IQueryableProvider
{
    IQueryable<TEntity> GetQueryable<TEntity>() where TEntity : class;
}
