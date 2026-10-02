using EFCore.DynamicQuery.Querying;
using EFCore.DynamicQuery.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace EFCore.DynamicQuery.Tests.Querying;

public class IncludingQueryExtensionsTests
{
    [Fact]
    public void Without_include_the_navigation_property_is_not_populated()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);

        var book = context.Books.AsNoTracking().Single(b => b.Id == 1);

        Assert.Null(book.Author);
    }

    [Fact]
    public void IncludeEntities_loads_the_navigation_a_mapped_property_crosses()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();
        var typeMap = mapper.GetTypeMap(typeof(Book), typeof(BookModel));

        // AuthorName maps to src.Author.Name - IncludeEntities must recognize "Author" as the
        // navigation path this field needs and apply .Include(b => b.Author) for it.
        var query = context.Books.AsNoTracking().IncludeEntities(["AuthorName"], typeMap);
        var book = query.Single(b => b.Id == 1);

        Assert.NotNull(book.Author);
        Assert.Equal("J.R.R. Tolkien", book.Author.Name);
    }

    [Fact]
    public void IncludeEntities_object_based_overload_matches_the_generic_one()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();
        var typeMap = mapper.GetTypeMap(typeof(Book), typeof(BookModel));

        // IQueryable<T> is covariant - assigning IQueryable<Book> to an IQueryable<object>
        // variable keeps the real underlying query typed as Book (not a literal object
        // projection), which is what the reflection-based Include machinery requires.
        IQueryable<object> query = context.Books.AsNoTracking();
        query = query.IncludeEntities(typeof(Book), ["AuthorName"], typeMap);

        var book = (Book)query.Single(b => ((Book)b).Id == 1);

        Assert.NotNull(book.Author);
        Assert.Equal("J.R.R. Tolkien", book.Author.Name);
    }

    [Fact]
    public void A_directly_mapped_non_navigation_field_requires_no_include()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();
        var typeMap = mapper.GetTypeMap(typeof(Book), typeof(BookModel));

        var query = context.Books.AsNoTracking().IncludeEntities(["Title"], typeMap);
        var book = query.Single(b => b.Id == 1);

        Assert.Null(book.Author);
    }
}
