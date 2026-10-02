using EFCore.DynamicQuery.SqlServer.Exceptions;
using EFCore.DynamicQuery.SqlServer.Querying;
using EFCore.DynamicQuery.SqlServer.Tests.TestSupport;

namespace EFCore.DynamicQuery.SqlServer.Tests.Querying;

public class TemporalQueryExtensionsTests
{
    [Fact]
    public void UseHistory_with_null_date_returns_the_source_unchanged_even_for_a_non_temporal_entity()
    {
        using var context = TestHelpers.CreateContext();
        context.Entities.Add(new TestEntity { Id = 1, Name = "Alpha" });
        context.SaveChanges();

        // No date given - must short-circuit before the temporal check, so this must not throw
        // even though TestEntity isn't mapped as temporal.
        var result = context.Entities.UseHistory(context, null).ToList();

        Assert.Single(result);
    }

    [Fact]
    public void UseHistory_throws_for_a_non_temporal_entity_when_a_date_is_given()
    {
        using var context = TestHelpers.CreateContext();

        Assert.Throws<EntityDoesNotHaveHistoricalDataTableException>(
            () => context.Entities.UseHistory(context, DateTime.UtcNow));
    }

    [Fact]
    public void UseAllHistory_throws_for_a_non_temporal_entity()
    {
        using var context = TestHelpers.CreateContext();

        Assert.Throws<EntityDoesNotHaveHistoricalDataTableException>(
            () => context.Entities.UseAllHistory(context));
    }
}
