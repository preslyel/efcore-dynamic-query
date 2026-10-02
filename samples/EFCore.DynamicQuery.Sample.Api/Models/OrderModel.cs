namespace EFCore.DynamicQuery.Sample.Api.Models;

public class OrderModel
{
    public long Id { get; set; }
    public int? CustomerId { get; set; }
    public DateTime? OrderDate { get; set; }
    public decimal? Amount { get; set; }
    public int? Status { get; set; }

    public int? TotalItems { get; set; }
    public string ItemsList { get; set; }

    // Not a direct Order property - mapped across the Customer navigation, demonstrating
    // IncludeEntities picking up the right .Include() automatically.
    public string? CustomerName { get; set; }
}
