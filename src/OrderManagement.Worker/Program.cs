using OrderManagement.Worker;
using OrderManagement.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<OrderCreatedProcessor>();

builder.Services.AddHostedService<RabbitMqOrderCreatedWorker>();

var host = builder.Build();
host.Run();
