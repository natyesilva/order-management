using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Api.Observability;
using OrderManagement.Api.Readiness;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Services;
using OrderManagement.Infrastructure;
using OrderManagement.Infrastructure.Messaging;
using OrderManagement.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin());
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("postgres")
    .AddCheck<ServiceBusHealthCheck>("azure_service_bus");

var app = builder.Build();

app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerPathFeature>();
        var ex = feature?.Error;

        var problem = new ProblemDetails
        {
            Title = "Unexpected error",
            Status = StatusCodes.Status500InternalServerError,
            Detail = app.Environment.IsDevelopment() ? ex?.ToString() : null,
            Instance = context.Request.Path
        };

        context.Response.StatusCode = problem.Status.Value;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem);
    });
});

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHealthChecks("/health");

// Auto-apply migrations on startup (MVP-friendly). In real prod you might separate this responsibility.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    if (MessagingTransport.IsServiceBus(app.Configuration))
    {
        // Nice DX: create queue if credentials allow it.
        var admin = scope.ServiceProvider.GetRequiredService<ServiceBusAdministrationClientWrapper>().Client;
        var queueName = app.Configuration["AZURE_SERVICE_BUS_QUEUE_NAME"] ?? "orders";
        if (admin is not null)
        {
            try
            {
                if (!await admin.QueueExistsAsync(queueName))
                    await admin.CreateQueueAsync(queueName);
            }
            catch
            {
                // If the credentials don't have management rights, skip silently.
            }
        }
    }
}

app.Run();
