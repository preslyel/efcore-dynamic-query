namespace EFCore.DynamicQuery.Sample.Api.Models;

public class OrderItemModel
{
    public long Id { get; set; }
    public long? OrderId { get; set; }
    public string? ProductName { get; set; }
    public int? Quantity { get; set; }
    public decimal? Price { get; set; }
}
