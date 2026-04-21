using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.Orders;

public sealed record OrderStatusHistoryResponse(
    Guid Id,
    OrderStatus? PreviousStatus,
    OrderStatus NewStatus,
    DateTimeOffset ChangedAt,
    string Source
);

