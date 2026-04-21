using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.Orders;

public sealed record OrderListItemResponse(
    Guid Id,
    string Customer,
    string Product,
    decimal Value,
    OrderStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);

