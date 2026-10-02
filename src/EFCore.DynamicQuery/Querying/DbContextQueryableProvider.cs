using Microsoft.EntityFrameworkCore;

namespace EFCore.DynamicQuery.Querying;

/// <summary>Default <see cref="IQueryableProvider"/> - a plain, untracked <c>DbSet&lt;TEntity&gt;</c> query.</summary>
public sealed class DbContextQueryableProvider(DbContext context) : IQueryableProvider
{
    public IQueryable<TEntity> GetQueryable<TEntity>() where TEntity : class => context.Set<TEntity>().AsNoTracking();
}
