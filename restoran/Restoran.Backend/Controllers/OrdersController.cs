using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restoran.Backend.DTOs;
using Restoran.Backend.Services;

namespace Restoran.Backend.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize(Roles = "Customer")]
public class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;

    public OrdersController(OrderService orderService) => _orderService = orderService;

    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderDto dto)
    {
        var (order, error) = await _orderService.CreateOrderAsync(GetUserId(), dto);
        return order == null ? BadRequest(new { message = error }) : Ok(order);
    }

    [HttpGet("history")]
    public async Task<ActionResult<OrderPageDto>> GetHistory([FromQuery] OrderFilterDto filter)
        => Ok(await _orderService.GetOrderHistoryAsync(GetUserId(), filter));

    [HttpGet("current")]
    public async Task<ActionResult<OrderDto>> GetCurrentOrder()
    {
        var order = await _orderService.GetCurrentOrderAsync(GetUserId());
        return order == null ? NotFound("Активных заказов нет") : Ok(order);
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var (success, error) = await _orderService.CancelOrderAsync(id, GetUserId());
        return success ? NoContent() : BadRequest(new { message = error });
    }

    [HttpPost("{id}/repeat")]
    public async Task<IActionResult> Repeat(Guid id)
    {
        var (success, error) = await _orderService.RepeatOrderAsync(id, GetUserId());
        return success ? NoContent() : BadRequest(new { message = error });
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? throw new InvalidOperationException());
}
