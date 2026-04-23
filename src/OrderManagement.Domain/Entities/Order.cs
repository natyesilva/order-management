using OrderManagement.Domain.Enums;

namespace OrderManagement.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }

    public string Customer { get; set; } = string.Empty;
    public string Product { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal TotalValue { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public List<OrderStatusHistory> StatusHistory { get; set; } = new();
}
