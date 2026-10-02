using EFCore.DynamicQuery.Filtering.Filters;
using EFCore.DynamicQuery.Tests.TestSupport;

namespace EFCore.DynamicQuery.Tests.Filtering.Filters;

public class LikeFilterTests
{
    // LikeFilter builds an EF.Functions.Like(...) call, which throws if ever invoked outside
    // real query translation - so this must run through an actual EF Core query provider, not
    // a directly-compiled-and-invoked delegate.

    [Fact]
    public void Matches_a_SQL_LIKE_pattern()
    {
        using var context = FilterQueryTestHelpers.CreateContext();
        context.Entities.AddRange(
            new FilterTestEntity { Id = 1, Name = "Widget-Small" },
            new FilterTestEntity { Id = 2, Name = "Widget-Large" },
            new FilterTestEntity { Id = 3, Name = "Gadget" });
        context.SaveChanges();

        var filter = new LikeFilter { Name = "Name", Value = "Widget%" };
        var result = FilterQueryTestHelpers.ApplyFilter(context.Entities, filter, "Name");

        Assert.Equal([1, 2], result.Select(e => e.Id).Order());
    }

    [Fact]
    public void Null_value_matches_only_entities_with_a_null_property()
    {
        using var context = FilterQueryTestHelpers.CreateContext();
        context.Entities.AddRange(
            new FilterTestEntity { Id = 1, Name = null },
            new FilterTestEntity { Id = 2, Name = "Widget" });
        context.SaveChanges();

        var filter = new LikeFilter { Name = "Name", Value = null };
        var result = FilterQueryTestHelpers.ApplyFilter(context.Entities, filter, "Name");

        Assert.Equal([1], result.Select(e => e.Id));
    }

    [Fact]
    public void FilterKey_is_like()
    {
        Assert.Equal("like", new LikeFilter { Name = "x" }.FilterKey);
    }
}
