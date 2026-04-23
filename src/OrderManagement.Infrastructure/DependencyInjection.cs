using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Services;
using OrderManagement.Infrastructure.Messaging;
using OrderManagement.Infrastructure.Persistence;
using Azure.Messaging.ServiceBus;

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

        var transport = (configuration["ORDER_MESSAGING_TRANSPORT"] ?? "rabbitmq").Trim().ToLowerInvariant();

        if (transport is "servicebus" or "asb" or "azure-service-bus")
        {
            var options = ServiceBusOptions.From(configuration);
            if (string.IsNullOrWhiteSpace(options.ConnectionString))
                throw new InvalidOperationException("Azure Service Bus transport selected, but AZURE_SERVICEBUS_CONNECTION_STRING is missing.");

            services.AddSingleton(options);
            services.AddSingleton(_ => new ServiceBusClient(options.ConnectionString));
            services.AddScoped<IOrderEventPublisher, AzureServiceBusOrderEventPublisher>();
        }
        else
        {
            services.AddSingleton<RabbitMqConnectionFactory>();
            services.AddScoped<IOrderEventPublisher, RabbitMqOrderEventPublisher>();
        }

        return services;
    }
}
