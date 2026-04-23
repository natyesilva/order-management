namespace OrderManagement.Infrastructure.Messaging;

using Microsoft.Extensions.Configuration;

public sealed class RabbitMqOptions
{
    public string Host { get; init; } = "rabbitmq";
    public int Port { get; init; } = 5672;
    public string Username { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public string QueueName { get; init; } = "orders";

    public static RabbitMqOptions From(IConfiguration configuration)
    {
        var port = 5672;
        var rawPort = configuration["RABBITMQ_PORT"];
        if (!string.IsNullOrWhiteSpace(rawPort) && int.TryParse(rawPort, out var parsed))
            port = parsed;

        return new RabbitMqOptions
        {
            Host = configuration["RABBITMQ_HOST"] ?? "rabbitmq",
            Port = port,
            Username = configuration["RABBITMQ_USERNAME"] ?? "guest",
            Password = configuration["RABBITMQ_PASSWORD"] ?? "guest",
            QueueName = configuration["RABBITMQ_QUEUE_NAME"] ?? "orders",
        };
    }
}
