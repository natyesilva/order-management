using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.Orders;

public sealed record OrderListItemResponse(
    Guid Id,
    string Customer,
    string Product,
    decimal Value,
    int Quantity,
    decimal TotalValue,
    OrderStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);
