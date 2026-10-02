using EFCore.DynamicQuery.Mapping;
using Microsoft.EntityFrameworkCore;

namespace EFCore.DynamicQuery.Tests.TestSupport;

// Mirrors a real-world case that broke against an actual SQL Server table (bigint primary key) -
// GetDataById must work for any Id type, not just int.
public class LongIdEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
}

public class LongIdModel
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
}

public class LongIdProfile : MappingProfile
{
    public LongIdProfile()
    {
        CreateMap<LongIdEntity, LongIdModel>();
    }
}

public sealed class LongIdTestDbContext(DbContextOptions<LongIdTestDbContext> options) : DbContext(options)
{
    public DbSet<LongIdEntity> Entities => Set<LongIdEntity>();
}
