using EFCore.DynamicQuery.Mapping;
using Microsoft.EntityFrameworkCore;

namespace EFCore.DynamicQuery.SqlServer.Tests.TestSupport;

// Deliberately NOT configured as temporal - these tests target the guard-clause behavior
// (EntityDoesNotHaveHistoricalDataTableException) and the null-asOf/no-history composition
// paths, which don't need a real temporal table. Real TemporalAsOf/TemporalAll translation is
// SQL-Server-only and was verified live against a real database instead (see session notes).
public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class TestModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class TestProfile : MappingProfile
{
    public TestProfile()
    {
        CreateMap<TestEntity, TestModel>()
            .ForMember(dest => dest.Id, src => src.Id, cfg => cfg.IsKey = true);
    }
}

public sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<TestEntity> Entities => Set<TestEntity>();
}
