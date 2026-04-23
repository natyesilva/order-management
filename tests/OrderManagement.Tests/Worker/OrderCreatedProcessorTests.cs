using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Messages;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.Tests.Fakes;
using OrderManagement.Worker;
using Xunit;

namespace OrderManagement.Tests.Worker;

public sealed class OrderCreatedProcessorTests
{
    [Fact]
    public async Task ProcessAsync_WhenPending_TransitionsToProcessingAndCompleted_AndWritesHistory()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(connection));
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<AppDbContext>());

        var clock = new TestClock(DateTimeOffset.Parse("2026-04-23T00:00:00Z"));
        services.AddSingleton<IClock>(clock);
        services.AddSingleton(new OrderProcessingOptions { TransitionDelaySeconds = 0 });
        services.AddSingleton(NullLogger<OrderCreatedProcessor>.Instance);

        await using var sp = services.BuildServiceProvider();

        await using (var scope = sp.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync();

            var order = new Order
            {
                Id = Guid.NewGuid(),
                Customer = "Acme",
                Product = "Produto X",
                Value = 10.50m,
                Quantity = 1,
                TotalValue = 10.50m,
                Status = OrderStatus.Pending,
                CreatedAt = clock.UtcNow
            };
            order.StatusHistory.Add(new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                PreviousStatus = null,
                NewStatus = OrderStatus.Pending,
                ChangedAt = clock.UtcNow,
                Source = "api"
            });

            db.Add(order);
            await db.SaveChangesAsync();
        }

        var processor = new OrderCreatedProcessor(sp, clock, new OrderProcessingOptions { TransitionDelaySeconds = 0 }, NullLogger<OrderCreatedProcessor>.Instance);

        var payload = new OrderCreatedEvent(
            OrderId: await GetOrderId(sp),
            Customer: "Acme",
            Product: "Produto X",
            Value: 10.50m,
            Quantity: 1,
            TotalValue: 10.50m,
            CreatedAtUtc: clock.UtcNow);

        await processor.ProcessAsync(payload, messageId: "msg-1", correlationId: payload.OrderId.ToString(), CancellationToken.None);

        await using (var scope = sp.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var order = await db.Orders.Include(o => o.StatusHistory).SingleAsync(o => o.Id == payload.OrderId);

            Assert.Equal(OrderStatus.Completed, order.Status);
            Assert.True(order.StatusHistory.Count >= 3);
            Assert.Contains(order.StatusHistory, h => h.NewStatus == OrderStatus.Processing);
            Assert.Contains(order.StatusHistory, h => h.NewStatus == OrderStatus.Completed);
        }
    }

    [Fact]
    public async Task ProcessAsync_WhenMessageAlreadyProcessed_DoesNothing()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(connection));
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<AppDbContext>());

        var clock = new TestClock(DateTimeOffset.Parse("2026-04-23T00:00:00Z"));
        services.AddSingleton<IClock>(clock);
        services.AddSingleton(new OrderProcessingOptions { TransitionDelaySeconds = 0 });
        services.AddSingleton(NullLogger<OrderCreatedProcessor>.Instance);

        await using var sp = services.BuildServiceProvider();

        var orderId = Guid.NewGuid();

        await using (var scope = sp.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync();

            db.ProcessedMessages.Add(new ProcessedMessage
            {
                Id = Guid.NewGuid(),
                MessageId = "msg-dup",
                CorrelationId = orderId.ToString(),
                EventType = "OrderCreated",
                ProcessedAt = clock.UtcNow
            });

            db.Orders.Add(new Order
            {
                Id = orderId,
                Customer = "Acme",
                Product = "Produto X",
                Value = 10.50m,
                Quantity = 1,
                TotalValue = 10.50m,
                Status = OrderStatus.Pending,
                CreatedAt = clock.UtcNow
            });

            await db.SaveChangesAsync();
        }

        var processor = new OrderCreatedProcessor(sp, clock, new OrderProcessingOptions { TransitionDelaySeconds = 0 }, NullLogger<OrderCreatedProcessor>.Instance);

        var payload = new OrderCreatedEvent(orderId, "Acme", "Produto X", 10.50m, 1, 10.50m, clock.UtcNow);
        await processor.ProcessAsync(payload, messageId: "msg-dup", correlationId: orderId.ToString(), CancellationToken.None);

        await using (var scope = sp.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var order = await db.Orders.SingleAsync(o => o.Id == orderId);
            Assert.Equal(OrderStatus.Pending, order.Status);
        }
    }

    private static async Task<Guid> GetOrderId(ServiceProvider sp)
    {
        await using var scope = sp.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Orders.Select(o => o.Id).SingleAsync();
    }
}
