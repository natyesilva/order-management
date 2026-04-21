using OrderManagement.Domain.Enums;

namespace OrderManagement.Domain.Entities;

public class OrderStatusHistory
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    public OrderStatus? PreviousStatus { get; set; }
    public OrderStatus NewStatus { get; set; }

    public DateTimeOffset ChangedAt { get; set; }
    public string Source { get; set; } = string.Empty;
}

