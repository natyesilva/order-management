namespace OrderManagement.Application.Messages;

public sealed record OrderCreatedEvent(
    Guid OrderId,
    string Customer,
    string Product,
    decimal Value,
    int Quantity,
    decimal TotalValue,
    DateTimeOffset CreatedAtUtc
);
