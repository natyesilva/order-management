using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Messages;
using OrderManagement.Infrastructure.Messaging;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Worker;

public sealed class OutboxOrderCreatedWorker(
    IServiceProvider services,
    IConfiguration configuration,
    OrderCreatedProcessor processor,
    ILogger<OutboxOrderCreatedWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!MessagingTransport.IsOutbox(configuration))
        {
            logger.LogInformation("Outbox worker is disabled. Transport={Transport}", MessagingTransport.Get(configuration));
            return;
        }

        logger.LogInformation("Outbox worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processedAny = await ProcessBatchAsync(stoppingToken);
                if (!processedAny)
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutdown
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox worker loop error.");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    private async Task<bool> ProcessBatchAsync(CancellationToken stoppingToken)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await using var tx = await db.Database.BeginTransactionAsync(stoppingToken);

        var messages = await db.OutboxMessages
            .FromSqlRaw("""
                SELECT *
                FROM outbox_messages
                WHERE processed_at IS NULL
                  AND event_type = 'OrderCreated'
                ORDER BY created_at
                LIMIT 10
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(stoppingToken);

        if (messages.Count == 0)
        {
            await tx.CommitAsync(stoppingToken);
            return false;
        }

        foreach (var message in messages)
        {
            using var _ = logger.BeginScope(new Dictionary<string, object>
            {
                ["correlationId"] = message.CorrelationId,
                ["messageId"] = message.MessageId
            });

            OrderCreatedEvent? payload;
            try
            {
                payload = JsonSerializer.Deserialize<OrderCreatedEvent>(message.Payload);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Invalid outbox payload. Marking processed to avoid retry loop.");
                message.ProcessedAt = DateTimeOffset.UtcNow;
                continue;
            }

            if (payload is null)
            {
                logger.LogError("Outbox payload is null. Marking processed to avoid retry loop.");
                message.ProcessedAt = DateTimeOffset.UtcNow;
                continue;
            }

            await processor.ProcessAsync(payload, message.MessageId, message.CorrelationId, stoppingToken);
            message.ProcessedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(stoppingToken);
        await tx.CommitAsync(stoppingToken);
        return true;
    }
}
