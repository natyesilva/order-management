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
            return HealthCheckResult.Healthy("Usando o transporte outbox do Postgres.");

        var cs = configuration["AZURE_SERVICE_BUS_CONNECTION_STRING"];
        if (string.IsNullOrWhiteSpace(cs))
            return HealthCheckResult.Unhealthy("Azure Service Bus não está configurado.");

        var client = adminWrapper.Client;
        if (client is null)
            return HealthCheckResult.Unhealthy("Não foi possível criar o cliente de administração do Azure Service Bus.");

        var queueName = configuration["AZURE_SERVICE_BUS_QUEUE_NAME"] ?? "orders";

        try
        {
            _ = await client.GetQueueRuntimePropertiesAsync(queueName, cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (RequestFailedException ex)
        {
            return HealthCheckResult.Unhealthy("Falha na verificação do Azure Service Bus.", ex);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Falha na verificação do Azure Service Bus.", ex);
        }
    }
}
