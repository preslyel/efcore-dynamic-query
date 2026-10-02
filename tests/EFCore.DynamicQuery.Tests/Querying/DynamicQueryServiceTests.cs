using EFCore.DynamicQuery.Filtering;
using EFCore.DynamicQuery.Querying;
using EFCore.DynamicQuery.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace EFCore.DynamicQuery.Tests.Querying;

public class DynamicQueryServiceTests
{
    private sealed class BookQueryableProvider(QueryTestDbContext context) : IQueryableProvider
    {
        public IQueryable<TEntity> GetQueryable<TEntity>() where TEntity : class =>
            (IQueryable<TEntity>)context.Books.AsNoTracking();
    }

    [Fact]
    public async Task GetQueryDataResultAsync_resolves_the_entity_type_from_the_model_alone()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();
        var service = new DynamicQueryService(new BookQueryableProvider(context), mapper);

        // Only BookModel is named here - DynamicQueryService figures out it needs a Book
        // queryable via mapper.GetSourceType<BookModel>(), with no repository written
        // specifically for Book/BookModel.
        var result = await service.GetQueryDataResultAsync<BookModel>(
            new PageableFilterDataModel { OrderBy = "Id", Ascending = true, Fields = ["Title"] });

        Assert.Equal(4, result.TotalCount);
        Assert.Equal("The Hobbit", result.Result!.First().Title);
    }

    [Fact]
    public void GetQueryDataResultAsync_without_filter_maps_everything()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();
        var service = new DynamicQueryService(new BookQueryableProvider(context), mapper);

        Assert.Equal(4, service.GetQueryDataResultAsync<BookModel>().Count);
    }

    [Fact]
    public void GetDataById_resolves_the_entity_type_from_the_model_alone()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();
        var service = new DynamicQueryService(new BookQueryableProvider(context), mapper);

        var book = Assert.Single(service.GetDataById<BookModel, int>(1));
        Assert.Equal("The Hobbit", book.Title);
    }

    [Fact]
    public async Task GetQueryDataResultAsync_honors_cancellation()
    {
        using var context = QueryTestHelpers.CreateContext();
        QueryTestHelpers.SeedBooks(context);
        var mapper = QueryTestHelpers.CreateMapper();
        var service = new DynamicQueryService(new BookQueryableProvider(context), mapper);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.GetQueryDataResultAsync<BookModel>(new PageableFilterDataModel(), cts.Token));
    }
}
