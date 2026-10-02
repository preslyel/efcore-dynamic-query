using System.Text.Json;
using EFCore.DynamicQuery.Filtering;
using EFCore.DynamicQuery.Filtering.Filters;
using EFCore.DynamicQuery.Querying;
using EFCore.DynamicQuery.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace EFCore.DynamicQuery.Tests.Querying;

public class QueryExtensionsTests
{
    [Fact]
    public async Task GetDynamicData_filters_orders_and_maps()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();

        var filterData = new PageableFilterDataModel
        {
            Filters = [new EqualFilter { Name = "AuthorName", Value = JsonSerializer.SerializeToElement("George Orwell") }],
            OrderBy = "Price",
            Ascending = true,
            Fields = ["Title", "Price", "AuthorName"]
        };

        var result = await context.Books.AsNoTracking().GetDynamicData<Book, BookModel>(filterData, mapper);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(["Animal Farm", "1984"], result.Result!.Select(b => b.Title));
        Assert.All(result.Result!, b => Assert.Equal("George Orwell", b.AuthorName));
    }

    [Fact]
    public async Task GetDynamicData_pages_results()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();

        var filterData = new PageableFilterDataModel
        {
            OrderBy = "Id",
            Ascending = true,
            Fields = ["Title"],
            PageIndex = 2,
            PageSize = 2
        };

        var result = await context.Books.AsNoTracking().GetDynamicData<Book, BookModel>(filterData, mapper);

        Assert.Equal(4, result.TotalCount);
        Assert.Equal(["1984", "Animal Farm"], result.Result!.Select(b => b.Title));
    }

    [Fact]
    public async Task GetDynamicData_applies_transforms_when_requested()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();

        var filterData = new PageableFilterDataModel
        {
            OrderBy = "Id",
            Fields = ["Title", "Price"],
            ApplyTransforms = true
        };

        var result = await context.Books.AsNoTracking().GetDynamicData<Book, BookModel>(filterData, mapper);

        Assert.Equal(20m, result.Result!.Single(b => b.Title == "The Hobbit").Price);
    }

    [Fact]
    public async Task GetDynamicData_filters_by_the_transformed_value_when_ApplyTransforms_is_set()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();

        // The client only ever sees transformed Price values here (ApplyTransforms: true), so it
        // filters using 20 - the displayed value for The Hobbit - with no way to know the raw
        // stored value is 10. Both the WHERE clause and the returned Price must agree.
        var filterData = new PageableFilterDataModel
        {
            Filters = [new EqualFilter { Name = "Price", Value = JsonSerializer.SerializeToElement(20m) }],
            Fields = ["Title", "Price"],
            ApplyTransforms = true
        };

        var result = await context.Books.AsNoTracking().GetDynamicData<Book, BookModel>(filterData, mapper);

        var book = Assert.Single(result.Result!);
        Assert.Equal("The Hobbit", book.Title);
        Assert.Equal(20m, book.Price);
    }

    [Fact]
    public async Task GetDynamicData_grouping_reflects_transformed_values_without_double_transforming()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();

        var filterData = new PageableFilterDataModel
        {
            GroupBy = "Price",
            OrderBy = "Price",
            Ascending = true,
            ApplyTransforms = true
        };

        var result = await context.Books.AsNoTracking().GetDynamicData<Book, BookModel>(filterData, mapper);

        // Raw prices are 6, 8, 10, 12 -> transformed (x2) should be 12, 16, 20, 24 - not 24/32/40/48
        // (which double-transforming would produce).
        Assert.Equal([12m, 16m, 20m, 24m], result.Result!.Select(b => b.Price));
    }

    [Fact]
    public async Task GetDynamicData_groups_by_a_mapped_property()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();

        var filterData = new PageableFilterDataModel
        {
            GroupBy = "AuthorName",
            OrderBy = "AuthorName",
            Ascending = true
        };

        var result = await context.Books.AsNoTracking().GetDynamicData<Book, BookModel>(filterData, mapper);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(["George Orwell", "J.R.R. Tolkien"], result.Result!.Select(b => b.AuthorName));
    }

    [Fact]
    public void GetDynamicData_without_filter_data_maps_every_row()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();

        var result = context.Books.AsNoTracking().GetDynamicData<Book, BookModel>(mapper);

        Assert.Equal(4, result.Count);
        Assert.Contains(result, b => b.Title == "1984" && b.AuthorName == "George Orwell");
    }

    [Fact]
    public void GetDataById_fetches_and_maps_a_single_entity()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();

        var result = context.Books.AsNoTracking().GetDataById<Book, BookModel, int>(1, applyTransforms: false, mapper);

        var book = Assert.Single(result);
        Assert.Equal("The Hobbit", book.Title);
        Assert.Equal("J.R.R. Tolkien", book.AuthorName);
        Assert.Equal(10m, book.Price);
    }

    [Fact]
    public void GetDataById_applies_transforms_when_requested()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();

        var result = context.Books.AsNoTracking().GetDataById<Book, BookModel, int>(1, applyTransforms: true, mapper);

        Assert.Equal(20m, Assert.Single(result).Price);
    }

    [Fact]
    public void GetDataById_works_for_a_non_int_Id_type()
    {
        var options = new DbContextOptionsBuilder<LongIdTestDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using var context = new LongIdTestDbContext(options);
        context.Entities.AddRange(
            new LongIdEntity { Id = 1, Name = "First" },
            new LongIdEntity { Id = 5_000_000_000, Name = "Second" });
        context.SaveChanges();

        var mapper = QueryTestHelpers.CreateMapper();
        var result = context.Entities.AsNoTracking().GetDataById<LongIdEntity, LongIdModel, long>(5_000_000_000, applyTransforms: false, mapper);

        Assert.Equal("Second", Assert.Single(result).Name);
    }
}
