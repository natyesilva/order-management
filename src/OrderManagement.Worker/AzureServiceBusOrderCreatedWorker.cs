using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderManagement.Application.Messages;
using OrderManagement.Infrastructure.Messaging;

namespace OrderManagement.Worker;

public sealed class AzureServiceBusOrderCreatedWorker(
    ServiceBusClient client,
    ServiceBusOptions options,
    OrderCreatedProcessor processor,
    ILogger<AzureServiceBusOrderCreatedWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var processorOptions = new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 1,
        };

        await using var sbProcessor = client.CreateProcessor(options.QueueName, processorOptions);

        sbProcessor.ProcessMessageAsync += async args =>
        {
            var msg = args.Message;
            var messageId = msg.MessageId;
            var correlationId = msg.CorrelationId;

            using var _scope = logger.BeginScope(new Dictionary<string, object>
            {
                ["messageId"] = messageId,
                ["correlationId"] = correlationId ?? ""
            });

            var eventType = msg.Subject
                ?? (msg.ApplicationProperties.TryGetValue("EventType", out var v) ? v?.ToString() : null);

            if (!string.Equals(eventType, "OrderCreated", StringComparison.Ordinal))
            {
                logger.LogWarning("Ignorando mensagem com EventType/Subject={EventType}", eventType);
                await args.CompleteMessageAsync(msg, stoppingToken);
                return;
            }

            OrderCreatedEvent? payload;
            try
            {
                payload = JsonSerializer.Deserialize<OrderCreatedEvent>(msg.Body);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Payload inválido. Mensagem será descartada (complete).");
                await args.CompleteMessageAsync(msg, stoppingToken);
                return;
            }

            if (payload is null)
            {
                logger.LogError("Payload nulo. Mensagem será descartada (complete).");
                await args.CompleteMessageAsync(msg, stoppingToken);
                return;
            }

            using var _orderScope = logger.BeginScope(new Dictionary<string, object>
            {
                ["orderId"] = payload.OrderId,
                ["eventType"] = eventType ?? ""
            });

            try
            {
                await processor.ProcessAsync(payload, messageId, correlationId, stoppingToken);
                await args.CompleteMessageAsync(msg, stoppingToken);
            }
            catch (DbUpdateException ex)
            {
                logger.LogWarning(ex, "DbUpdateException ao processar mensagem; complete para manter idempotência.");
                await args.CompleteMessageAsync(msg, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutting down
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao processar mensagem; abandon.");
                await args.AbandonMessageAsync(msg, cancellationToken: stoppingToken);
            }
        };

        sbProcessor.ProcessErrorAsync += args =>
        {
            logger.LogError(args.Exception, "Erro no ServiceBusProcessor. EntityPath={EntityPath}", args.EntityPath);
            return Task.CompletedTask;
        };

        logger.LogInformation("Worker Azure Service Bus iniciado. Queue={Queue}", options.QueueName);

        await sbProcessor.StartProcessingAsync(stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        finally
        {
            await sbProcessor.StopProcessingAsync(CancellationToken.None);
        }
    }
}

