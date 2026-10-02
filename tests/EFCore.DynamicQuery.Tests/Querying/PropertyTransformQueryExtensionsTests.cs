using EFCore.DynamicQuery.Querying;
using EFCore.DynamicQuery.Tests.TestSupport;

namespace EFCore.DynamicQuery.Tests.Querying;

public class PropertyTransformQueryExtensionsTests
{
    [Fact]
    public void Applies_the_MutableFunction_to_the_configured_property()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();
        var typeMap = mapper.GetTypeMap(typeof(Book), typeof(BookModel));

        // BookProfile configures Price's MutableFunction as p => p * 2m.
        var book = context.Books.GetTransformedSelect(typeMap).Single(b => b.Id == 1);

        Assert.Equal(20m, book.Price);
    }

    [Fact]
    public void Leaves_properties_without_a_MutableFunction_unchanged()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();
        var typeMap = mapper.GetTypeMap(typeof(Book), typeof(BookModel));

        var book = context.Books.GetTransformedSelect(typeMap).Single(b => b.Id == 1);

        Assert.Equal("The Hobbit", book.Title);
        Assert.Equal(1, book.AuthorId);
    }
}
