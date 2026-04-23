using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OrderManagement.Infrastructure.Messaging;

namespace OrderManagement.Api.Readiness;

public sealed class AzureServiceBusHealthCheck(
    ServiceBusClient client,
    ServiceBusOptions options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Attempt a lightweight operation: peek a message.
            // If connection string is invalid or namespace unreachable, this will throw.
            var receiver = client.CreateReceiver(options.QueueName);
            _ = await receiver.PeekMessageAsync(cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Azure Service Bus indisponível.", ex);
        }
    }
}
