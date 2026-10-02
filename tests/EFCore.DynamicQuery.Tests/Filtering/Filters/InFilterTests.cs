using System.Text.Json;
using EFCore.DynamicQuery.Filtering.Filters;
using EFCore.DynamicQuery.Tests.TestSupport;

namespace EFCore.DynamicQuery.Tests.Filtering.Filters;

public class InFilterTests
{
    [Fact]
    public void Matches_any_value_in_the_list()
    {
        using var context = FilterQueryTestHelpers.CreateContext();
        context.Entities.AddRange(
            new FilterTestEntity { Id = 1, Name = "Widget" },
            new FilterTestEntity { Id = 2, Name = "Gadget" },
            new FilterTestEntity { Id = 3, Name = "Sprocket" });
        context.SaveChanges();

        var filter = new InFilter
        {
            Name = "Name",
            Values = [JsonSerializer.SerializeToElement("Widget"), JsonSerializer.SerializeToElement("Gadget")]
        };
        var result = FilterQueryTestHelpers.ApplyFilter(context.Entities, filter, "Name");

        Assert.Equal([1, 2], result.Select(e => e.Id).Order());
    }

    [Fact]
    public void Empty_list_matches_nothing()
    {
        using var context = FilterQueryTestHelpers.CreateContext();
        context.Entities.Add(new FilterTestEntity { Id = 1, Name = "Widget" });
        context.SaveChanges();

        var filter = new InFilter { Name = "Name", Values = [] };
        var result = FilterQueryTestHelpers.ApplyFilter(context.Entities, filter, "Name");

        Assert.Empty(result);
    }

    [Fact]
    public void Works_against_a_nullable_property_including_null_rows()
    {
        using var context = FilterQueryTestHelpers.CreateContext();
        context.Entities.AddRange(
            new FilterTestEntity { Id = 1, OptionalQuantity = 5 },
            new FilterTestEntity { Id = 2, OptionalQuantity = null },
            new FilterTestEntity { Id = 3, OptionalQuantity = 6 });
        context.SaveChanges();

        var filter = new InFilter { Name = "OptionalQuantity", Values = [JsonSerializer.SerializeToElement(5)] };
        var result = FilterQueryTestHelpers.ApplyFilter(context.Entities, filter, "OptionalQuantity");

        Assert.Equal([1], result.Select(e => e.Id));
    }
}
