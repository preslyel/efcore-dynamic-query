using EFCore.DynamicQuery.Querying;
using EFCore.DynamicQuery.Tests.TestSupport;

namespace EFCore.DynamicQuery.Tests.Querying;

public class WhereInQueryExtensionsTests
{
    [Fact]
    public void WhereIn_matches_any_value_in_the_set()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);

        var result = context.Books.WhereIn(b => b.Id, new[] { 1, 3 }).ToList();

        Assert.Equal([1, 3], result.Select(b => b.Id).Order());
    }

    [Fact]
    public void WhereIn_with_empty_values_matches_nothing()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);

        var result = context.Books.WhereIn(b => b.Id, Array.Empty<int>()).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void WhereIn_with_an_or_condition_includes_rows_matching_either()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);

        var result = context.Books.WhereIn(b => b.Id, new[] { 1 }, b => b.Title == "1984").ToList();

        Assert.Equal([1, 3], result.Select(b => b.Id).Order());
    }

    [Fact]
    public void WhereNotIn_excludes_every_value_in_the_set()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);

        var result = context.Books.WhereNotIn(b => b.Id, new[] { 1, 2 }).ToList();

        Assert.Equal([3, 4], result.Select(b => b.Id).Order());
    }

    [Fact]
    public void WhereNotIn_with_empty_values_returns_everything()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);

        var result = context.Books.WhereNotIn(b => b.Id, Array.Empty<int>()).ToList();

        Assert.Equal(4, result.Count);
    }
}
