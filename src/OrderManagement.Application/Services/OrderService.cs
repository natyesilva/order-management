using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Messages;
using OrderManagement.Application.Orders;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.Services;

public sealed class OrderService(
    DbContext dbContext,
    IClock clock,
    IOrderEventPublisher publisher,
    ILogger<OrderService> logger) : IOrderService
{
    // Note: to keep layering simple for this take-home, Application depends on an EF DbContext abstraction.
    // Infrastructure provides the concrete AppDbContext (registered as DbContext).
    private readonly DbContext _db = dbContext;

    public async Task<OrderResponse> CreateAsync(CreateOrderRequest request, string correlationId, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Customer = request.Customer.Trim(),
            Product = request.Product.Trim(),
            Value = request.Value,
            Status = OrderStatus.Pending,
            CreatedAt = now
        };

        order.StatusHistory.Add(new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            PreviousStatus = null,
            NewStatus = OrderStatus.Pending,
            ChangedAt = now,
            Source = "api"
        });

        _db.Add(order);
        await _db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Order created: {OrderId}", order.Id);

        await publisher.PublishAsync(
            new OrderCreatedEvent(order.Id, order.Customer, order.Product, order.Value, order.CreatedAt),
            cancellationToken);

        return await GetByIdAsync(order.Id, cancellationToken)
            ?? throw new InvalidOperationException("Order was created but could not be reloaded.");
    }

    public async Task<IReadOnlyList<OrderListItemResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _db.Set<Order>()
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderListItemResponse(
                o.Id,
                o.Customer,
                o.Product,
                o.Value,
                o.Status,
                o.CreatedAt,
                o.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<OrderResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await _db.Set<Order>()
            .AsNoTracking()
            .Include(o => o.StatusHistory.OrderBy(h => h.ChangedAt))
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order is null) return null;

        return new OrderResponse(
            order.Id,
            order.Customer,
            order.Product,
            order.Value,
            order.Status,
            order.CreatedAt,
            order.UpdatedAt,
            order.StatusHistory
                .OrderBy(h => h.ChangedAt)
                .Select(h => new OrderStatusHistoryResponse(h.Id, h.PreviousStatus, h.NewStatus, h.ChangedAt, h.Source))
                .ToList());
    }
}

