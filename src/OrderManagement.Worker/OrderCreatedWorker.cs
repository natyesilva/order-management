using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Messages;
using OrderManagement.Infrastructure.Messaging;

namespace OrderManagement.Worker;

public sealed class OrderCreatedWorker(
    IConfiguration configuration,
    ServiceBusClient sbClient,
    OrderCreatedProcessor processor,
    ILogger<OrderCreatedWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!MessagingTransport.IsServiceBus(configuration))
        {
            logger.LogInformation("Service Bus worker is disabled. Transport={Transport}", MessagingTransport.Get(configuration));
            return;
        }

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
        var messageId = args.Message.MessageId;
        var correlationId = args.Message.CorrelationId;
        using var scope = logger.BeginScope(new Dictionary<string, object> { ["correlationId"] = correlationId ?? "", ["messageId"] = messageId });

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

        try
        {
            await processor.ProcessAsync(payload, messageId, correlationId, stoppingToken);

            await args.CompleteMessageAsync(args.Message, stoppingToken);
        }
        catch (DbUpdateException ex)
        {
            // If we raced on the unique MessageId, treat as idempotent success.
            logger.LogWarning(ex, "DbUpdateException while processing message; attempting to complete message.");
            await args.CompleteMessageAsync(args.Message, stoppingToken);
        }
    }
}
