using EFCore.DynamicQuery.Filtering;
using EFCore.DynamicQuery.Querying;

namespace EFCore.DynamicQuery.Tests.Querying;

public class PagingQueryExtensionsTests
{
    private static readonly IQueryable<int> Source = Enumerable.Range(1, 10).AsQueryable();

    [Fact]
    public void GetPagedData_with_zero_page_size_returns_everything()
    {
        Assert.Equal(Source, Source.GetPagedData(pageIndex: 0, pageSize: 0));
    }

    [Fact]
    public void GetPagedData_page_index_zero_and_one_both_mean_the_first_page()
    {
        Assert.Equal(Source.GetPagedData(0, 3), Source.GetPagedData(1, 3));
        Assert.Equal([1, 2, 3], Source.GetPagedData(1, 3));
    }

    [Fact]
    public void GetPagedData_second_page()
    {
        Assert.Equal([4, 5, 6], Source.GetPagedData(2, 3));
    }

    [Fact]
    public void GetPagedData_from_PageableFilterDataModel_delegates_to_the_int_overload()
    {
        var request = new PageableFilterDataModel { PageIndex = 2, PageSize = 3 };

        Assert.Equal(Source.GetPagedData(2, 3), Source.GetPagedData(request));
    }

    [Fact]
    public void TakeIfNotNull_takes_when_given_a_value()
    {
        Assert.Equal([1, 2], Source.TakeIfNotNull(2));
    }

    [Fact]
    public void TakeIfNotNull_passes_through_when_null()
    {
        Assert.Equal(Source, Source.TakeIfNotNull(null));
    }

    [Fact]
    public void SkipIfNotNull_skips_when_given_a_value()
    {
        Assert.Equal([9, 10], Source.SkipIfNotNull(8));
    }

    [Fact]
    public void SkipIfNotNull_passes_through_when_null()
    {
        Assert.Equal(Source, Source.SkipIfNotNull(null));
    }
}
