using Microsoft.Extensions.Diagnostics.HealthChecks;
using OrderManagement.Infrastructure.Messaging;
using RabbitMQ.Client.Exceptions;

namespace OrderManagement.Api.Readiness;

public sealed class RabbitMqHealthCheck(
    RabbitMqConnectionFactory connectionFactory) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = connectionFactory.CreateConnection();
            using var channel = connection.CreateModel();

            var q = connectionFactory.Options.QueueName;
            channel.QueueDeclare(q, durable: true, exclusive: false, autoDelete: false, arguments: null);

            return Task.FromResult(HealthCheckResult.Healthy());
        }
        catch (BrokerUnreachableException ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("RabbitMQ indisponível.", ex));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Falha na verificação do RabbitMQ.", ex));
        }
    }
}

