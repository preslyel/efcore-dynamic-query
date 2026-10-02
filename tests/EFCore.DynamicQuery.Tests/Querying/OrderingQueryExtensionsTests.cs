using EFCore.DynamicQuery.Querying;
using EFCore.DynamicQuery.Tests.TestSupport;

namespace EFCore.DynamicQuery.Tests.Querying;

public class OrderingQueryExtensionsTests
{
    [Fact]
    public void Empty_orderBy_returns_the_query_unchanged()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();
        var typeMap = mapper.GetTypeMap(typeof(Book), typeof(BookModel));

        var result = context.Books.GetOrderedData("", true, typeMap).ToList();

        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void Orders_by_a_directly_mapped_property_ascending()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();
        var typeMap = mapper.GetTypeMap(typeof(Book), typeof(BookModel));

        var result = context.Books.GetOrderedData("Price", true, typeMap).ToList();

        Assert.Equal([4, 3, 1, 2], result.Select(b => b.Id));
    }

    [Fact]
    public void Orders_by_a_directly_mapped_property_descending()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();
        var typeMap = mapper.GetTypeMap(typeof(Book), typeof(BookModel));

        var result = context.Books.GetOrderedData("Price", false, typeMap).ToList();

        Assert.Equal([2, 1, 3, 4], result.Select(b => b.Id));
    }

    [Fact]
    public void Orders_by_the_minimum_value_of_a_collection_valued_mapped_property()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();
        var typeMap = mapper.GetTypeMap(typeof(Book), typeof(BookModel));

        // ReviewRatings maps to src.Reviews.Select(r => r.Rating) - collection-valued, so
        // GetOrderedData must order by Min() across each book's reviews. Restricted to books
        // that actually have reviews (1, 2, 3) - Book 4 has none, see the dedicated test below
        // for what happens there.
        var withReviews = context.Books.Where(b => b.Id != 4);
        var result = withReviews.GetOrderedData("ReviewRatings", true, typeMap).ToList();

        // Min ratings: Book 1 -> 4, Book 2 -> 3, Book 3 -> 1
        Assert.Equal([3, 2, 1], result.Select(b => b.Id));
    }

    [Fact]
    public void Ordering_by_a_collection_property_with_an_empty_collection_row_present()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();
        var typeMap = mapper.GetTypeMap(typeof(Book), typeof(BookModel));

        // Book 4 has zero reviews - Min() over an empty sequence is undefined per plain LINQ
        // semantics (SQL's MIN() would instead return NULL for zero rows). Documenting the
        // actual behavior here rather than assuming one way or the other.
        var exception = Record.Exception(() => context.Books.GetOrderedData("ReviewRatings", true, typeMap).ToList());

        Assert.NotNull(exception);
        Assert.IsType<InvalidOperationException>(exception);
    }
}
