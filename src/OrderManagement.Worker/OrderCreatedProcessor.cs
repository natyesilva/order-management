using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Messages;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Worker;

public sealed class OrderCreatedProcessor(
    IServiceProvider services,
    IClock clock,
    ILogger<OrderCreatedProcessor> logger)
{
    public async Task ProcessAsync(
        OrderCreatedEvent payload,
        string messageId,
        string? correlationId,
        CancellationToken stoppingToken)
    {
        await using var scopeServices = services.CreateAsyncScope();
        var db = scopeServices.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = clock.UtcNow;

        await using var tx = await db.Database.BeginTransactionAsync(stoppingToken);

        var already = await db.ProcessedMessages
            .AsNoTracking()
            .AnyAsync(x => x.MessageId == messageId, stoppingToken);

        if (already)
        {
            logger.LogInformation("Message already processed. Skipping.");
            await tx.CommitAsync(stoppingToken);
            return;
        }

        var order = await db.Orders
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == payload.OrderId, stoppingToken);

        if (order is null)
        {
            logger.LogWarning("Order not found for message. Marking processed. OrderId={OrderId}", payload.OrderId);
            db.ProcessedMessages.Add(new ProcessedMessage
            {
                Id = Guid.NewGuid(),
                MessageId = messageId,
                CorrelationId = correlationId ?? payload.OrderId.ToString(),
                EventType = "OrderCreated",
                ProcessedAt = now
            });
            await db.SaveChangesAsync(stoppingToken);
            await tx.CommitAsync(stoppingToken);
            return;
        }

        if (order.Status == OrderStatus.Pending)
        {
            Transition(order, OrderStatus.Processing, now, "worker");
            await db.SaveChangesAsync(stoppingToken);

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            now = clock.UtcNow;
            Transition(order, OrderStatus.Completed, now, "worker");
            await db.SaveChangesAsync(stoppingToken);
        }
        else
        {
            logger.LogInformation("Order already transitioned. CurrentStatus={Status}", order.Status);
        }

        db.ProcessedMessages.Add(new ProcessedMessage
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            CorrelationId = correlationId ?? payload.OrderId.ToString(),
            EventType = "OrderCreated",
            ProcessedAt = now
        });

        await db.SaveChangesAsync(stoppingToken);
        await tx.CommitAsync(stoppingToken);
    }

    private static void Transition(Order order, OrderStatus newStatus, DateTimeOffset changedAt, string source)
    {
        if (order.Status == newStatus) return;

        var previous = order.Status;
        order.Status = newStatus;
        order.UpdatedAt = changedAt;
        order.StatusHistory.Add(new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            PreviousStatus = previous,
            NewStatus = newStatus,
            ChangedAt = changedAt,
            Source = source
        });
    }
}

