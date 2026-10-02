using System.Text.Json;
using EFCore.DynamicQuery.Filtering.Filters;
using EFCore.DynamicQuery.Tests.TestSupport;

namespace EFCore.DynamicQuery.Tests.Filtering.Filters;

public class NotEqualFilterTests
{
    [Fact]
    public void Matches_values_that_differ()
    {
        using var context = FilterQueryTestHelpers.CreateContext();
        context.Entities.AddRange(
            new FilterTestEntity { Id = 1, Name = "Widget" },
            new FilterTestEntity { Id = 2, Name = "Gadget" });
        context.SaveChanges();

        var filter = new NotEqualFilter { Name = "Name", Value = JsonSerializer.SerializeToElement("Widget") };
        var result = FilterQueryTestHelpers.ApplyFilter(context.Entities, filter, "Name");

        Assert.Equal([2], result.Select(e => e.Id));
    }

    [Fact]
    public void Nullable_property_not_equal_to_null_matches_only_entities_with_a_value()
    {
        using var context = FilterQueryTestHelpers.CreateContext();
        context.Entities.AddRange(
            new FilterTestEntity { Id = 1, OptionalQuantity = null },
            new FilterTestEntity { Id = 2, OptionalQuantity = 5 });
        context.SaveChanges();

        var filter = new NotEqualFilter { Name = "OptionalQuantity", Value = null };
        var result = FilterQueryTestHelpers.ApplyFilter(context.Entities, filter, "OptionalQuantity");

        Assert.Equal([2], result.Select(e => e.Id));
    }
}
