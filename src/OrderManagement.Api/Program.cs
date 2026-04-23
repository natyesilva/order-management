using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using OrderManagement.Api.Observability;
using OrderManagement.Api.Readiness;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Services;
using OrderManagement.Infrastructure;
using OrderManagement.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Ensure enums (OrderStatus) are serialized as strings so the UI shows "Pending" etc (not 0/1/2).
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
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
    .AddCheck<RabbitMqHealthCheck>("rabbitmq");

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

    // Defensive DX fallback: if migrations are missing at runtime (or failed to apply),
    // the DB can end up with only "__EFMigrationsHistory" and no domain tables.
    // In that case, drop the history table and create the schema from the current model.
    var ordersTableExists = await db.Database
        .SqlQueryRaw<bool>("SELECT to_regclass('public.orders') IS NOT NULL AS \"Value\"")
        .SingleAsync();

    if (!ordersTableExists)
    {
        try
        {
            // If the DB ended up in a partial-migrations state, ensure we start from a clean schema.
            // This is acceptable for this MVP/local setup (Compose) and avoids "relation does not exist" at runtime.
            await db.Database.ExecuteSqlRawAsync("DROP SCHEMA IF EXISTS public CASCADE;");
            await db.Database.ExecuteSqlRawAsync("CREATE SCHEMA public;");
        }
        catch
        {
            // best effort
        }

        await db.Database.EnsureCreatedAsync();
    }

}

app.Run();
