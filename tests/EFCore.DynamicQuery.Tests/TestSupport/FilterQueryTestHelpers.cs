using System.Linq.Expressions;
using EFCore.DynamicQuery.Filtering;
using Microsoft.EntityFrameworkCore;

namespace EFCore.DynamicQuery.Tests.TestSupport;

/// <summary>
/// Runs a filter's GetBinaryExpression through a real EF Core query (InMemory provider) rather
/// than compiling and invoking it directly - filters like LikeFilter build EF.Functions.Like
/// calls that throw if ever invoked outside query translation, and nullable-property access
/// patterns some filters use behave differently under SQL/EF translation than under plain
/// LINQ-to-Objects. Going through a real IQueryable is what these filters are actually meant for.
/// </summary>
internal static class FilterQueryTestHelpers
{
    public static FilterTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FilterTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FilterTestDbContext(options);
    }

    public static List<FilterTestEntity> ApplyFilter(IQueryable<FilterTestEntity> query, IFilterItem filter, string propertyName)
    {
        var parameter = Expression.Parameter(typeof(FilterTestEntity), "x");
        var property = Expression.Property(parameter, propertyName);
        var predicate = Expression.Lambda<Func<FilterTestEntity, bool>>(filter.GetBinaryExpression(property), parameter);

        return [.. query.Where(predicate)];
    }

    public static List<FilterTestEntity> ApplyObjectLevelFilter(IQueryable<FilterTestEntity> query, IFilterItem filter)
    {
        var parameter = Expression.Parameter(typeof(FilterTestEntity), "x");
        var predicate = Expression.Lambda<Func<FilterTestEntity, bool>>(filter.GetBinaryExpression(parameter), parameter);

        return [.. query.Where(predicate)];
    }
}
