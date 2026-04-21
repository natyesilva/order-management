using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace OrderManagement.Api.Readiness;

public sealed class PostgresHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var cs = configuration.GetConnectionString("Postgres") ?? configuration["POSTGRES_CONNECTION_STRING"];
        if (string.IsNullOrWhiteSpace(cs))
            return HealthCheckResult.Unhealthy("Postgres connection string is missing.");

        try
        {
            await using var conn = new NpgsqlConnection(cs);
            await conn.OpenAsync(cancellationToken);
            await using var cmd = new NpgsqlCommand("SELECT 1", conn);
            await cmd.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Postgres check failed.", ex);
        }
    }
}

