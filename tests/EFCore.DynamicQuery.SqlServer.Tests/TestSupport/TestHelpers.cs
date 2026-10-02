using EFCore.DynamicQuery.Mapping;
using Microsoft.EntityFrameworkCore;

namespace EFCore.DynamicQuery.SqlServer.Tests.TestSupport;

internal static class TestHelpers
{
    public static TestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
    }

    public static IDynamicMapper CreateMapper() => new DynamicMapper([typeof(TestProfile).Assembly]);
}
