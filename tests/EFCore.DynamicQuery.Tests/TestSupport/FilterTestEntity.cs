using Microsoft.EntityFrameworkCore;

namespace EFCore.DynamicQuery.Tests.TestSupport;

public class FilterTestEntity
{
    public int Id { get; set; }
    public string? Name { get; set; } = "";
    public int Quantity { get; set; }
    public int? OptionalQuantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Country { get; set; } = "";
    public string Region { get; set; } = "";
}

public sealed class FilterTestDbContext(DbContextOptions<FilterTestDbContext> options) : DbContext(options)
{
    public DbSet<FilterTestEntity> Entities => Set<FilterTestEntity>();
}
