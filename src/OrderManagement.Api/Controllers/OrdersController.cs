using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.Observability;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Orders;

namespace OrderManagement.Api.Controllers;

[ApiController]
[Route("orders")]
public sealed class OrdersController(IOrderService orders) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        // For end-to-end tracing, we set CorrelationId = OrderId for the creation flow.
        var orderId = Guid.NewGuid();
        var correlationId = orderId.ToString();

        HttpContext.Items[CorrelationIdMiddleware.HeaderName] = correlationId;
        Response.Headers[CorrelationIdMiddleware.HeaderName] = correlationId;

        var created = await orders.CreateAsync(request, orderId, correlationId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OrderListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var list = await orders.GetAllAsync(cancellationToken);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(id, cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }
}
