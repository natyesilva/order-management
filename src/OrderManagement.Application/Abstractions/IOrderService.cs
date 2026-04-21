using OrderManagement.Application.Orders;

namespace OrderManagement.Application.Abstractions;

public interface IOrderService
{
    Task<OrderResponse> CreateAsync(CreateOrderRequest request, string correlationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OrderListItemResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<OrderResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}

