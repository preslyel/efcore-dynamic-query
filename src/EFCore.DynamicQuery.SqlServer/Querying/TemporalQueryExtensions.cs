using EFCore.DynamicQuery.SqlServer.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EFCore.DynamicQuery.SqlServer.Querying;

/// <summary>Convenience wrappers over EF Core's SQL Server temporal-table ("history") querying.</summary>
public static class TemporalQueryExtensions
{
    /// <summary>Queries the entity as it was at <paramref name="dateTime"/>, or the current data if <c>null</c>.</summary>
    public static IQueryable<TEntity> UseHistory<TEntity>(this DbSet<TEntity> source, DbContext dbContext, DateTime? dateTime)
        where TEntity : class
    {
        if (!dateTime.HasValue)
            return source;

        RequireTemporal<TEntity>(dbContext);

        return source.TemporalAsOf(dateTime.Value);
    }

    /// <summary>Queries every historical version of every row.</summary>
    public static IQueryable<TEntity> UseAllHistory<TEntity>(this DbSet<TEntity> source, DbContext dbContext)
        where TEntity : class
    {
        RequireTemporal<TEntity>(dbContext);

        return source.TemporalAll();
    }

    private static void RequireTemporal<TEntity>(DbContext dbContext)
    {
        var isTemporal = dbContext.Model.FindEntityType(typeof(TEntity))?.IsTemporal() ?? false;

        if (!isTemporal)
            throw new EntityDoesNotHaveHistoricalDataTableException(typeof(TEntity));
    }
}
