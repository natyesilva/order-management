namespace OrderManagement.Infrastructure.Messaging;

using Microsoft.Extensions.Configuration;

public static class MessagingTransport
{
    public const string ConfigKey = "ORDER_MESSAGING_TRANSPORT";

    public static string Get(IConfiguration configuration)
        => (configuration[ConfigKey] ?? "outbox").Trim().ToLowerInvariant();

    public static bool IsOutbox(IConfiguration configuration) => Get(configuration) == "outbox";

    public static bool IsServiceBus(IConfiguration configuration) => Get(configuration) == "servicebus";
}
