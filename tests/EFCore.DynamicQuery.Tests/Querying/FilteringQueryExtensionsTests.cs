using System.Text.Json;
using EFCore.DynamicQuery.Filtering.Filters;
using EFCore.DynamicQuery.Querying;
using EFCore.DynamicQuery.Tests.TestSupport;

namespace EFCore.DynamicQuery.Tests.Querying;

public class FilteringQueryExtensionsTests
{
    [Fact]
    public void Filters_by_a_directly_mapped_property()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();
        var typeMap = mapper.GetTypeMap(typeof(Book), typeof(BookModel));

        var filter = new EqualFilter { Name = "Title", Value = JsonSerializer.SerializeToElement("1984") };
        var result = context.Books.GetFilteredData([filter], typeMap).ToList();

        Assert.Equal([3], result.Select(b => b.Id));
    }

    [Fact]
    public void Filters_across_a_cross_navigation_mapped_property()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();
        var typeMap = mapper.GetTypeMap(typeof(Book), typeof(BookModel));

        // AuthorName maps to src.Author.Name - not a direct Book property.
        var filter = new EqualFilter { Name = "AuthorName", Value = JsonSerializer.SerializeToElement("George Orwell") };
        var result = context.Books.GetFilteredData([filter], typeMap).ToList();

        Assert.Equal([3, 4], result.Select(b => b.Id).Order());
    }

    [Fact]
    public void Filters_against_a_collection_valued_mapped_property_using_Any_semantics()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();
        var typeMap = mapper.GetTypeMap(typeof(Book), typeof(BookModel));

        // ReviewRatings maps to src.Reviews.Select(r => r.Rating) - a collection expression,
        // so InFilter here must route through BuildAnyPredicate (any review with rating 5).
        var filter = new InFilter { Name = "ReviewRatings", Values = [JsonSerializer.SerializeToElement(5)] };
        var result = context.Books.GetFilteredData([filter], typeMap).ToList();

        Assert.Equal([1, 3], result.Select(b => b.Id).Order());
    }

    [Fact]
    public void Filters_a_string_Join_over_a_collection_projection_via_Any_semantics()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();
        var typeMap = mapper.GetTypeMap(typeof(Book), typeof(BookModel));

        // ReviewRatingsJoined maps to string.Join(", ", src.Reviews.Select(r => r.Rating.ToString())).
        // IsCollectionExpression/UnrollChain special-case string.Join by looking at its own second
        // argument (the thing being joined), not the Join call's own string return type - so this
        // must still route through BuildAnyPredicate and match per-review, not compare against the
        // whole joined string. Book 1's reviews are [4, 5] (joined: "4, 5") and book 3's are [5, 1]
        // (joined: "5, 1") - neither whole string equals "5", but both contain a review rated 5.
        var filter = new EqualFilter { Name = "ReviewRatingsJoined", Value = JsonSerializer.SerializeToElement("5") };
        var result = context.Books.GetFilteredData([filter], typeMap).ToList();

        Assert.Equal([1, 3], result.Select(b => b.Id).Order());
    }

    [Fact]
    public void LikeFilter_without_wildcards_on_a_string_Join_over_a_collection_still_matches_per_element()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();
        var typeMap = mapper.GetTypeMap(typeof(Book), typeof(BookModel));

        // A plain (no %) LikeFilter value against a string.Join-mapped property still finds books
        // with ANY matching review, because BuildAnyPredicate applies the LIKE per-element (e.g.
        // EF.Functions.Like(r.Rating.ToString(), "5")) rather than against the joined blob - it
        // would NOT match "4, 5"/"5, 1" as substrings without this per-element routing.
        var filter = new LikeFilter { Name = "ReviewRatingsJoined", Value = "5" };
        var result = context.Books.GetFilteredData([filter], typeMap).ToList();

        Assert.Equal([1, 3], result.Select(b => b.Id).Order());
    }

    [Fact]
    public void Filtering_with_applyTransforms_matches_the_value_the_client_sees_not_the_raw_column()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();
        var typeMap = mapper.GetTypeMap(typeof(Book), typeof(BookModel));

        // BookProfile configures Price's MutableFunction as p => p * 2m, so a client that only
        // ever sees the transformed value (raw DB Price 10 -> displayed 20) filters using 20 -
        // it has no way to know the raw stored value is 10. With applyTransforms: true,
        // GetFilteredData must apply the same MutableFunction to the column before comparing,
        // not compare the raw column against the client's (transformed-space) value.
        var filter = new EqualFilter { Name = "Price", Value = JsonSerializer.SerializeToElement(20m) };
        var result = context.Books.GetFilteredData([filter], typeMap, applyTransforms: true).ToList();

        Assert.Equal([1], result.Select(b => b.Id));
    }

    [Fact]
    public void Filtering_without_applyTransforms_compares_against_the_raw_stored_value()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();
        var typeMap = mapper.GetTypeMap(typeof(Book), typeof(BookModel));

        // Default (applyTransforms: false, matching GetFilteredData's default) - filtering stays
        // in raw-storage space, consistent with results also being raw when transforms aren't
        // requested. Book 1's raw Price is 10, not the transformed 20.
        var filter = new EqualFilter { Name = "Price", Value = JsonSerializer.SerializeToElement(10m) };
        var result = context.Books.GetFilteredData([filter], typeMap).ToList();

        Assert.Equal([1], result.Select(b => b.Id));
    }

    [Fact]
    public void No_filters_returns_the_query_unchanged()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();
        var typeMap = mapper.GetTypeMap(typeof(Book), typeof(BookModel));

        var result = context.Books.GetFilteredData([], typeMap).ToList();

        Assert.Equal(4, result.Count);
    }
}
