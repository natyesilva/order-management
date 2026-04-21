using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Messages;

namespace OrderManagement.Infrastructure.Messaging;

public sealed class MissingServiceBusOrderEventPublisher : IOrderEventPublisher
{
    public Task PublishAsync(OrderCreatedEvent message, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Azure Service Bus is not configured. Set AZURE_SERVICE_BUS_CONNECTION_STRING.");
}

