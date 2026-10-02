using System.Text.Json;
using EFCore.DynamicQuery.Filtering.Filters;
using EFCore.DynamicQuery.Tests.TestSupport;

namespace EFCore.DynamicQuery.Tests.Filtering.Filters;

public class EqualFilterTests
{
    [Fact]
    public void Matches_equal_values_from_a_JsonElement_value()
    {
        using var context = FilterQueryTestHelpers.CreateContext();
        context.Entities.AddRange(
            new FilterTestEntity { Id = 1, Name = "Widget" },
            new FilterTestEntity { Id = 2, Name = "Gadget" });
        context.SaveChanges();

        var filter = new EqualFilter { Name = "Name", Value = JsonSerializer.SerializeToElement("Widget") };
        var result = FilterQueryTestHelpers.ApplyFilter(context.Entities, filter, "Name");

        Assert.Equal([1], result.Select(e => e.Id));
    }

    [Fact]
    public void Matches_equal_values_from_a_plain_CLR_value_built_without_JSON()
    {
        // A filter constructed directly in code (not via FilterJsonConverter) should work too -
        // DeserializeConstant must not assume every non-null Value is a JsonElement.
        using var context = FilterQueryTestHelpers.CreateContext();
        context.Entities.AddRange(
            new FilterTestEntity { Id = 1, Quantity = 5 },
            new FilterTestEntity { Id = 2, Quantity = 6 });
        context.SaveChanges();

        var filter = new EqualFilter { Name = "Quantity", Value = 5 };
        var result = FilterQueryTestHelpers.ApplyFilter(context.Entities, filter, "Quantity");

        Assert.Equal([1], result.Select(e => e.Id));
    }

    [Fact]
    public void Nullable_property_equal_to_null_matches_only_entities_without_a_value()
    {
        using var context = FilterQueryTestHelpers.CreateContext();
        context.Entities.AddRange(
            new FilterTestEntity { Id = 1, OptionalQuantity = null },
            new FilterTestEntity { Id = 2, OptionalQuantity = 5 });
        context.SaveChanges();

        var filter = new EqualFilter { Name = "OptionalQuantity", Value = null };
        var result = FilterQueryTestHelpers.ApplyFilter(context.Entities, filter, "OptionalQuantity");

        Assert.Equal([1], result.Select(e => e.Id));
    }

    [Fact]
    public void Nullable_property_equal_to_a_value_matches_only_that_value()
    {
        using var context = FilterQueryTestHelpers.CreateContext();
        context.Entities.AddRange(
            new FilterTestEntity { Id = 1, OptionalQuantity = 5 },
            new FilterTestEntity { Id = 2, OptionalQuantity = 6 },
            new FilterTestEntity { Id = 3, OptionalQuantity = null });
        context.SaveChanges();

        var filter = new EqualFilter { Name = "OptionalQuantity", Value = JsonSerializer.SerializeToElement(5) };
        var result = FilterQueryTestHelpers.ApplyFilter(context.Entities, filter, "OptionalQuantity");

        Assert.Equal([1], result.Select(e => e.Id));
    }

    [Fact]
    public void FilterKey_is_equal()
    {
        Assert.Equal("equal", new EqualFilter { Name = "x" }.FilterKey);
    }
}
