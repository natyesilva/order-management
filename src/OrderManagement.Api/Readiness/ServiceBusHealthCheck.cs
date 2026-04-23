using Azure;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OrderManagement.Infrastructure.Messaging;

namespace OrderManagement.Api.Readiness;

public sealed class ServiceBusHealthCheck(
    IConfiguration configuration,
    ServiceBusAdministrationClientWrapper adminWrapper) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (MessagingTransport.IsOutbox(configuration))
            return HealthCheckResult.Healthy("Using Postgres outbox transport.");

        var cs = configuration["AZURE_SERVICE_BUS_CONNECTION_STRING"];
        if (string.IsNullOrWhiteSpace(cs))
            return HealthCheckResult.Unhealthy("Azure Service Bus is not configured.");

        var client = adminWrapper.Client;
        if (client is null)
            return HealthCheckResult.Unhealthy("Azure Service Bus admin client could not be created.");

        var queueName = configuration["AZURE_SERVICE_BUS_QUEUE_NAME"] ?? "orders";

        try
        {
            _ = await client.GetQueueRuntimePropertiesAsync(queueName, cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (RequestFailedException ex)
        {
            return HealthCheckResult.Unhealthy("Azure Service Bus check failed.", ex);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Azure Service Bus check failed.", ex);
        }
    }
}
