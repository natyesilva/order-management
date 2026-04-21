using OrderManagement.Application.Messages;

namespace OrderManagement.Application.Abstractions;

public interface IOrderEventPublisher
{
    Task PublishAsync(OrderCreatedEvent message, CancellationToken cancellationToken);
}

