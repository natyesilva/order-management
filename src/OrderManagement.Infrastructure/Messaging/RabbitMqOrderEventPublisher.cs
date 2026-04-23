using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Messages;
using RabbitMQ.Client;

namespace OrderManagement.Infrastructure.Messaging;

public sealed class RabbitMqOrderEventPublisher(
    RabbitMqConnectionFactory connectionFactory,
    ILogger<RabbitMqOrderEventPublisher> logger) : IOrderEventPublisher
{
    private readonly RabbitMqOptions _options = connectionFactory.Options;

    public Task PublishAsync(OrderCreatedEvent message, CancellationToken cancellationToken)
    {
        var messageId = Guid.NewGuid().ToString("N");

        using var connection = connectionFactory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var body = JsonSerializer.SerializeToUtf8Bytes(message);

        var props = channel.CreateBasicProperties();
        props.ContentType = "application/json";
        props.DeliveryMode = 2; // persistent
        props.MessageId = messageId;
        props.CorrelationId = message.OrderId.ToString();
        props.Type = "OrderCreated";
        props.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        props.Headers = new Dictionary<string, object>
        {
            ["EventType"] = Encoding.UTF8.GetBytes("OrderCreated"),
        };

        channel.BasicPublish(
            exchange: "",
            routingKey: _options.QueueName,
            mandatory: false,
            basicProperties: props,
            body: body);

        logger.LogInformation("Evento OrderCreated publicado no RabbitMQ. OrderId={OrderId} MessageId={MessageId}", message.OrderId, messageId);

        return Task.CompletedTask;
    }
}

