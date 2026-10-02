using EFCore.DynamicQuery.Filtering;
using EFCore.DynamicQuery.SqlServer.Exceptions;
using EFCore.DynamicQuery.SqlServer.Querying;
using EFCore.DynamicQuery.SqlServer.Tests.TestSupport;

namespace EFCore.DynamicQuery.SqlServer.Tests.Querying;

public class TemporalDynamicDataExtensionsTests
{
    [Fact]
    public async Task GetDynamicDataAsOf_with_null_asOf_behaves_like_the_core_pipeline()
    {
        using var context = TestHelpers.CreateContext();
        context.Entities.AddRange(new TestEntity { Id = 1, Name = "Alpha" }, new TestEntity { Id = 2, Name = "Beta" });
        context.SaveChanges();
        var mapper = TestHelpers.CreateMapper();

        var result = await context.Entities.GetDynamicDataAsOf<TestEntity, TestModel>(
            context, null, new PageableFilterDataModel { OrderBy = "Id", Ascending = true }, mapper);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(["Alpha", "Beta"], result.Result!.Select(m => m.Name));
    }

    [Fact]
    public async Task GetDynamicDataAsOf_throws_for_a_non_temporal_entity_when_asOf_is_given()
    {
        using var context = TestHelpers.CreateContext();
        var mapper = TestHelpers.CreateMapper();

        await Assert.ThrowsAsync<EntityDoesNotHaveHistoricalDataTableException>(
            () => context.Entities.GetDynamicDataAsOf<TestEntity, TestModel>(
                context, DateTime.UtcNow, new PageableFilterDataModel(), mapper));
    }

    [Fact]
    public async Task GetAllHistoryDynamicData_throws_for_a_non_temporal_entity()
    {
        using var context = TestHelpers.CreateContext();
        var mapper = TestHelpers.CreateMapper();

        await Assert.ThrowsAsync<EntityDoesNotHaveHistoricalDataTableException>(
            () => context.Entities.GetAllHistoryDynamicData<TestEntity, TestModel>(
                context, new PageableFilterDataModel(), mapper));
    }

    [Fact]
    public void GetDataByIdAsOf_with_null_asOf_behaves_like_the_core_pipeline()
    {
        using var context = TestHelpers.CreateContext();
        context.Entities.Add(new TestEntity { Id = 1, Name = "Alpha" });
        context.SaveChanges();
        var mapper = TestHelpers.CreateMapper();

        var result = context.Entities.GetDataByIdAsOf<TestEntity, TestModel, int>(context, null, 1, applyTransforms: false, mapper);

        Assert.Equal("Alpha", Assert.Single(result).Name);
    }

    [Fact]
    public void GetDataByIdAsOf_throws_for_a_non_temporal_entity_when_asOf_is_given()
    {
        using var context = TestHelpers.CreateContext();
        var mapper = TestHelpers.CreateMapper();

        Assert.Throws<EntityDoesNotHaveHistoricalDataTableException>(
            () => context.Entities.GetDataByIdAsOf<TestEntity, TestModel, int>(context, DateTime.UtcNow, 1, applyTransforms: false, mapper));
    }
}
