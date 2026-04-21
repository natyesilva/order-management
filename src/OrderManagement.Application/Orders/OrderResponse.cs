using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.Orders;

public sealed record OrderResponse(
    Guid Id,
    string Customer,
    string Product,
    decimal Value,
    OrderStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<OrderStatusHistoryResponse> StatusHistory
);

