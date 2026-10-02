using EFCore.DynamicQuery.Filtering;
using EFCore.DynamicQuery.Querying;
using EFCore.DynamicQuery.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace EFCore.DynamicQuery.Tests.Querying;

public class DynamicQueryRepositoryTests
{
    [Fact]
    public async Task GetQueryDataResultAsync_with_filter_data_does_not_require_passing_the_mapper_per_call()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();

        var repository = new DynamicQueryRepository<Book>(context.Books.AsNoTracking(), mapper);

        var result = await repository.GetQueryDataResultAsync<BookModel>(
            new PageableFilterDataModel { OrderBy = "Id", Fields = ["Title"] });

        Assert.Equal(4, result.TotalCount);
    }

    [Fact]
    public void GetQueryDataResultAsync_without_filter_data_maps_everything()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();

        var repository = new DynamicQueryRepository<Book>(context.Books.AsNoTracking(), mapper);

        Assert.Equal(4, repository.GetQueryDataResultAsync<BookModel>().Count);
    }

    [Fact]
    public void GetDataById_fetches_and_maps_a_single_entity()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();

        var repository = new DynamicQueryRepository<Book>(context.Books.AsNoTracking(), mapper);

        var book = Assert.Single(repository.GetDataById<BookModel, int>(1));
        Assert.Equal("The Hobbit", book.Title);
    }
}
