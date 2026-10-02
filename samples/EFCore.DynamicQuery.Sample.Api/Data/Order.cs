namespace EFCore.DynamicQuery.Sample.Api.Data;

public class Order
{
    public long Id { get; set; }
    public int? CustomerId { get; set; }
    public DateTime? OrderDate { get; set; }
    public decimal? Amount { get; set; }
    public int? Status { get; set; }

    public Customer? Customer { get; set; }
    public List<OrderItem> OrderItems { get; set; } = [];
}
