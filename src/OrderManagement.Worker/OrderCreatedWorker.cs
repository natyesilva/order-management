using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Messages;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Worker;

public sealed class OrderCreatedWorker(
    IServiceProvider services,
    IConfiguration configuration,
    ServiceBusClient sbClient,
    IClock clock,
    ILogger<OrderCreatedWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var cs = configuration["AZURE_SERVICE_BUS_CONNECTION_STRING"];
        if (string.IsNullOrWhiteSpace(cs))
        {
            logger.LogError("AZURE_SERVICE_BUS_CONNECTION_STRING is not configured. Worker will not start processing.");
            return;
        }

        var queueName = configuration["AZURE_SERVICE_BUS_QUEUE_NAME"] ?? "orders";
        var processor = sbClient.CreateProcessor(queueName, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 1
        });

        processor.ProcessMessageAsync += args => HandleMessageAsync(args, stoppingToken);
        processor.ProcessErrorAsync += args =>
        {
            logger.LogError(args.Exception, "Service Bus processor error. Entity={EntityPath}", args.EntityPath);
            return Task.CompletedTask;
        };

        logger.LogInformation("Worker started. Queue={Queue}", queueName);
        await processor.StartProcessingAsync(stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // expected on shutdown
        }
        finally
        {
            await processor.StopProcessingAsync(CancellationToken.None);
            await processor.DisposeAsync();
        }
    }

    private async Task HandleMessageAsync(ProcessMessageEventArgs args, CancellationToken stoppingToken)
    {
        var correlationId = args.Message.CorrelationId;
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["correlationId"] = correlationId,
            ["messageId"] = args.Message.MessageId
        });

        var eventType = args.Message.ApplicationProperties.TryGetValue("EventType", out var et)
            ? et?.ToString()
            : null;

        if (!string.Equals(eventType, "OrderCreated", StringComparison.Ordinal))
        {
            logger.LogWarning("Ignoring message with unsupported EventType={EventType}", eventType);
            await args.CompleteMessageAsync(args.Message, stoppingToken);
            return;
        }

        OrderCreatedEvent? payload;
        try
        {
            payload = JsonSerializer.Deserialize<OrderCreatedEvent>(args.Message.Body);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Invalid message payload. Dead-lettering.");
            await args.DeadLetterMessageAsync(args.Message, "InvalidPayload", ex.Message, stoppingToken);
            return;
        }

        if (payload is null)
        {
            await args.DeadLetterMessageAsync(args.Message, "InvalidPayload", "Payload is null.", stoppingToken);
            return;
        }

        await using var scopeServices = services.CreateAsyncScope();
        var db = scopeServices.ServiceProvider.GetRequiredService<AppDbContext>();

        // Idempotency: persist MessageId as unique; if it already exists we consider it processed.
        var now = clock.UtcNow;
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(stoppingToken);

            var already = await db.ProcessedMessages
                .AsNoTracking()
                .AnyAsync(x => x.MessageId == args.Message.MessageId, stoppingToken);

            if (already)
            {
                logger.LogInformation("Message already processed. Completing.");
                await tx.CommitAsync(stoppingToken);
                await args.CompleteMessageAsync(args.Message, stoppingToken);
                return;
            }

            var order = await db.Orders
                .Include(o => o.StatusHistory)
                .FirstOrDefaultAsync(o => o.Id == payload.OrderId, stoppingToken);

            if (order is null)
            {
                logger.LogWarning("Order not found for message. Completing. OrderId={OrderId}", payload.OrderId);
                db.ProcessedMessages.Add(new ProcessedMessage
                {
                    Id = Guid.NewGuid(),
                    MessageId = args.Message.MessageId,
                    CorrelationId = correlationId ?? payload.OrderId.ToString(),
                    EventType = "OrderCreated",
                    ProcessedAt = now
                });
                await db.SaveChangesAsync(stoppingToken);
                await tx.CommitAsync(stoppingToken);
                await args.CompleteMessageAsync(args.Message, stoppingToken);
                return;
            }

            // Enforce status sequence: Pending -> Processing -> Completed
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
                MessageId = args.Message.MessageId,
                CorrelationId = correlationId ?? payload.OrderId.ToString(),
                EventType = "OrderCreated",
                ProcessedAt = now
            });

            await db.SaveChangesAsync(stoppingToken);
            await tx.CommitAsync(stoppingToken);

            await args.CompleteMessageAsync(args.Message, stoppingToken);
        }
        catch (DbUpdateException ex)
        {
            // If we raced on the unique MessageId, treat as idempotent success.
            logger.LogWarning(ex, "DbUpdateException while processing message; attempting to complete message.");
            await args.CompleteMessageAsync(args.Message, stoppingToken);
        }
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
