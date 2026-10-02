using System.Text.Json;
using EFCore.DynamicQuery.Filtering.Filters;
using EFCore.DynamicQuery.Tests.TestSupport;

namespace EFCore.DynamicQuery.Tests.Filtering.Filters;

public class NamedEqualFilterTests
{
    [Fact]
    public void ANDs_equality_across_every_named_sibling_property()
    {
        using var context = FilterQueryTestHelpers.CreateContext();
        context.Entities.AddRange(
            new FilterTestEntity { Id = 1, Country = "US", Region = "CA" },
            new FilterTestEntity { Id = 2, Country = "US", Region = "NY" },
            new FilterTestEntity { Id = 3, Country = "CA", Region = "CA" });
        context.SaveChanges();

        var filter = new NamedEqualFilter
        {
            Name = "CompositeKey",
            NameValues =
            [
                new() { Name = "Country", Value = JsonSerializer.SerializeToElement("US") },
                new() { Name = "Region", Value = JsonSerializer.SerializeToElement("CA") }
            ]
        };
        var result = FilterQueryTestHelpers.ApplyObjectLevelFilter(context.Entities, filter);

        Assert.Equal([1], result.Select(e => e.Id));
    }
}
