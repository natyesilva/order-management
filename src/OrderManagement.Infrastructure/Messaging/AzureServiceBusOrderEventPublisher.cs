using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Messages;

namespace OrderManagement.Infrastructure.Messaging;

public sealed class AzureServiceBusOrderEventPublisher(
    ServiceBusClient client,
    IConfiguration configuration,
    ILogger<AzureServiceBusOrderEventPublisher> logger) : IOrderEventPublisher
{
    private readonly string _queueName =
        configuration["AZURE_SERVICE_BUS_QUEUE_NAME"] ?? "orders";

    public async Task PublishAsync(OrderCreatedEvent message, CancellationToken cancellationToken)
    {
        var sender = client.CreateSender(_queueName);

        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        var sbMessage = new ServiceBusMessage(body)
        {
            ContentType = "application/json",
            CorrelationId = message.OrderId.ToString()
        };

        sbMessage.ApplicationProperties["EventType"] = "OrderCreated";

        await sender.SendMessageAsync(sbMessage, cancellationToken);

        logger.LogInformation("Published OrderCreated event to Service Bus. OrderId={OrderId}", message.OrderId);
    }
}

