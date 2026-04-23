using OrderManagement.Worker;
using OrderManagement.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<OrderCreatedProcessor>();

var transport = (builder.Configuration["ORDER_MESSAGING_TRANSPORT"] ?? "rabbitmq").Trim().ToLowerInvariant();
if (transport is "servicebus" or "asb" or "azure-service-bus")
{
    builder.Services.AddHostedService<AzureServiceBusOrderCreatedWorker>();
}
else
{
    builder.Services.AddHostedService<RabbitMqOrderCreatedWorker>();
}

var host = builder.Build();
host.Run();
