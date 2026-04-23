using OrderManagement.Worker;
using OrderManagement.Infrastructure.Messaging;
using OrderManagement.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<OrderCreatedProcessor>();

var transport = MessagingTransport.Get(builder.Configuration);
if (transport == "servicebus")
    builder.Services.AddHostedService<OrderCreatedWorker>();
else if (transport == "outbox")
    builder.Services.AddHostedService<OutboxOrderCreatedWorker>();
else
    throw new InvalidOperationException($"Unsupported {MessagingTransport.ConfigKey} value: '{transport}'. Use 'outbox' or 'servicebus'.");

var host = builder.Build();
host.Run();
