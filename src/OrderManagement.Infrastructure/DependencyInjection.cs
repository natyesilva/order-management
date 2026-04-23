using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Services;
using OrderManagement.Infrastructure.Messaging;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? configuration["POSTGRES_CONNECTION_STRING"];

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Postgres connection string is missing. Set ConnectionStrings:Postgres or POSTGRES_CONNECTION_STRING.");

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        // Register as DbContext for Application (keeps Application layer small).
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddSingleton<IClock, SystemClock>();

        services.AddSingleton<ServiceBusAdministrationClientWrapper>();

        var transport = MessagingTransport.Get(configuration);
        var sbConnectionString = configuration["AZURE_SERVICE_BUS_CONNECTION_STRING"];
        if (transport == "servicebus")
        {
            if (string.IsNullOrWhiteSpace(sbConnectionString))
                throw new InvalidOperationException("ORDER_MESSAGING_TRANSPORT=servicebus requires AZURE_SERVICE_BUS_CONNECTION_STRING.");

            services.AddSingleton(_ => new ServiceBusClient(sbConnectionString));
            services.AddScoped<IOrderEventPublisher, AzureServiceBusOrderEventPublisher>();
        }
        else if (transport == "outbox")
        {
            services.AddScoped<IOrderEventPublisher, PostgresOutboxOrderEventPublisher>();
        }
        else
        {
            throw new InvalidOperationException($"Unsupported {MessagingTransport.ConfigKey} value: '{transport}'. Use 'outbox' or 'servicebus'.");
        }

        return services;
    }
}
