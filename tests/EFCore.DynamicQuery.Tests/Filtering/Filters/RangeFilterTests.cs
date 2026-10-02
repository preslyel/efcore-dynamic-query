using System.Text.Json;
using EFCore.DynamicQuery.Filtering.Filters;
using EFCore.DynamicQuery.Tests.TestSupport;

namespace EFCore.DynamicQuery.Tests.Filtering.Filters;

public class RangeFilterTests
{
    [Fact]
    public void Matches_values_inclusive_of_both_bounds()
    {
        using var context = FilterQueryTestHelpers.CreateContext();
        context.Entities.AddRange(
            new FilterTestEntity { Id = 1, Quantity = 1 },
            new FilterTestEntity { Id = 2, Quantity = 5 },
            new FilterTestEntity { Id = 3, Quantity = 10 },
            new FilterTestEntity { Id = 4, Quantity = 15 });
        context.SaveChanges();

        var filter = new RangeFilter
        {
            Name = "Quantity",
            FromValue = JsonSerializer.SerializeToElement(5),
            ToValue = JsonSerializer.SerializeToElement(10)
        };
        var result = FilterQueryTestHelpers.ApplyFilter(context.Entities, filter, "Quantity");

        Assert.Equal([2, 3], result.Select(e => e.Id).Order());
    }

    [Fact]
    public void Works_against_a_nullable_property()
    {
        using var context = FilterQueryTestHelpers.CreateContext();
        context.Entities.AddRange(
            new FilterTestEntity { Id = 1, OptionalQuantity = 5 },
            new FilterTestEntity { Id = 2, OptionalQuantity = null },
            new FilterTestEntity { Id = 3, OptionalQuantity = 100 });
        context.SaveChanges();

        var filter = new RangeFilter
        {
            Name = "OptionalQuantity",
            FromValue = JsonSerializer.SerializeToElement(0),
            ToValue = JsonSerializer.SerializeToElement(10)
        };
        var result = FilterQueryTestHelpers.ApplyFilter(context.Entities, filter, "OptionalQuantity");

        Assert.Equal([1], result.Select(e => e.Id));
    }
}
