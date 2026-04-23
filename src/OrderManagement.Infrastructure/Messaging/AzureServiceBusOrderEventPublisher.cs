using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Messages;

namespace OrderManagement.Infrastructure.Messaging;

public sealed class AzureServiceBusOrderEventPublisher(
    ServiceBusClient client,
    ServiceBusOptions options,
    ILogger<AzureServiceBusOrderEventPublisher> logger) : IOrderEventPublisher
{
    public async Task PublishAsync(OrderCreatedEvent message, CancellationToken cancellationToken)
    {
        var eventType = "OrderCreated";
        var sender = client.CreateSender(options.QueueName);

        var body = JsonSerializer.SerializeToUtf8Bytes(message);

        var sbMessage = new ServiceBusMessage(body)
        {
            ContentType = "application/json",
            MessageId = Guid.NewGuid().ToString("N"),
            CorrelationId = message.OrderId.ToString(),
            Subject = eventType,
        };

        sbMessage.ApplicationProperties["EventType"] = eventType;

        await sender.SendMessageAsync(sbMessage, cancellationToken);

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["orderId"] = message.OrderId,
            ["correlationId"] = message.OrderId.ToString(),
            ["eventType"] = eventType,
            ["messageId"] = sbMessage.MessageId,
        }))
        {
            logger.LogInformation("Evento publicado no Azure Service Bus.");
        }
    }
}

