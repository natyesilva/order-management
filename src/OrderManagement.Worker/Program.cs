using OrderManagement.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<OrderCreatedWorker>();

var host = builder.Build();
host.Run();
