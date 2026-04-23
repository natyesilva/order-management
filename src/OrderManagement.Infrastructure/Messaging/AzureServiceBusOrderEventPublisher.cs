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
        var sender = client.CreateSender(options.QueueName);

        var body = JsonSerializer.SerializeToUtf8Bytes(message);

        var sbMessage = new ServiceBusMessage(body)
        {
            ContentType = "application/json",
            MessageId = Guid.NewGuid().ToString("N"),
            CorrelationId = message.OrderId.ToString(),
            Subject = "OrderCreated",
        };

        sbMessage.ApplicationProperties["EventType"] = "OrderCreated";

        await sender.SendMessageAsync(sbMessage, cancellationToken);

        logger.LogInformation(
            "Evento OrderCreated publicado no Azure Service Bus. OrderId={OrderId} MessageId={MessageId}",
            message.OrderId,
            sbMessage.MessageId);
    }
}

