using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Messages;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Messaging;

public sealed class PostgresOutboxOrderEventPublisher(DbContext db, IClock clock) : IOrderEventPublisher
{
    public async Task PublishAsync(OrderCreatedEvent message, CancellationToken cancellationToken)
    {
        var outbox = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageId = Guid.NewGuid().ToString("N"),
            CorrelationId = message.OrderId.ToString(),
            EventType = "OrderCreated",
            Payload = JsonSerializer.Serialize(message),
            CreatedAt = clock.UtcNow
        };

        db.Add(outbox);
        await db.SaveChangesAsync(cancellationToken);
    }
}

