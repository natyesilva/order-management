using Microsoft.Extensions.Configuration;

namespace OrderManagement.Infrastructure.Messaging;

public sealed class ServiceBusOptions
{
    public string ConnectionString { get; init; } = string.Empty;
    public string QueueName { get; init; } = "orders";

    public static ServiceBusOptions From(IConfiguration configuration)
    {
        return new ServiceBusOptions
        {
            ConnectionString = configuration["AZURE_SERVICEBUS_CONNECTION_STRING"] ?? string.Empty,
            QueueName = configuration["AZURE_SERVICEBUS_QUEUE_NAME"] ?? "orders",
        };
    }
}

