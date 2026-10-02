using Microsoft.EntityFrameworkCore;

namespace EFCore.DynamicQuery.Sample.Api.Data;

/// <summary>
/// Maps the three tables in the ProjectForge database's dbo schema - Customers, Orders,
/// OrderItems. Query-only: this context targets an existing, populated database, so it never
/// runs migrations or EnsureCreated.
/// </summary>
public sealed class ProjectForgeDbContext(DbContextOptions<ProjectForgeDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers", "dbo");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).HasMaxLength(100);
            entity.Property(c => c.Email).HasMaxLength(200);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders", "dbo");
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Amount).HasPrecision(18, 2);

            // No physical FK constraints exist on these tables (verified against the real
            // database) - relationships here are conventional (matching column names), so they
            // need to be configured explicitly rather than discovered from the schema.
            entity.HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems", "dbo");
            entity.HasKey(oi => oi.Id);
            entity.Property(oi => oi.ProductName).HasMaxLength(200);
            entity.Property(oi => oi.Price).HasPrecision(18, 2);

            entity.HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId);
        });
    }
}
