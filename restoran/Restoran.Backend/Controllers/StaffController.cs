using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restoran.Backend.DTOs;
using Restoran.Backend.Models;
using Restoran.Backend.Services;

namespace Restoran.Backend.Controllers;

[ApiController]
[Route("api/cook")]
[Authorize(Roles = "Cook")]
public class CookController : ControllerBase
{
    private readonly OrderService _orderService;
    private readonly RestaurantService _restaurantService;

    public CookController(OrderService orderService, RestaurantService restaurantService)
    {
        _orderService = orderService;
        _restaurantService = restaurantService;
    }

    [HttpGet("orders")]
    public async Task<ActionResult<OrderPageDto>> GetNewOrders([FromQuery] StaffOrderFilterDto filter)
    {
        var restaurantId = await _restaurantService.GetEmployeeRestaurantIdAsync(GetUserId());
        if (restaurantId == null) return BadRequest("Повар не привязан к ресторану");

        filter.Statuses = new List<OrderStatus> { OrderStatus.Created };
        return Ok(await _orderService.GetRestaurantOrdersAsync(restaurantId.Value, filter));
    }

    [HttpPost("orders/{orderId}/take")]
    public async Task<IActionResult> TakeOrder(Guid orderId)
    {
        var (success, error) = await _orderService.ChangeOrderStatusAsync(orderId, GetUserId(), OrderStatus.Kitchen, "Cook");
        return success ? NoContent() : BadRequest(new { message = error });
    }

    [HttpPost("orders/{orderId}/pack")]
    public async Task<IActionResult> PackOrder(Guid orderId)
    {
        var (success, error) = await _orderService.ChangeOrderStatusAsync(orderId, GetUserId(), OrderStatus.Packaging, "Cook");
        return success ? NoContent() : BadRequest(new { message = error });
    }

    [HttpGet("orders/history")]
    public async Task<ActionResult<OrderPageDto>> GetHistory([FromQuery] StaffOrderFilterDto filter)
    {
        var restaurantId = await _restaurantService.GetEmployeeRestaurantIdAsync(GetUserId());
        if (restaurantId == null) return BadRequest("Повар не привязан к ресторану");

        filter.Statuses = null;
        return Ok(await _orderService.GetRestaurantOrdersAsync(restaurantId.Value, filter));
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value!);
}

[ApiController]
[Route("api/manager")]
[Authorize(Roles = "Manager")]
public class ManagerController : ControllerBase
{
    private readonly OrderService _orderService;
    private readonly RestaurantService _restaurantService;

    public ManagerController(OrderService orderService, RestaurantService restaurantService)
    {
        _orderService = orderService;
        _restaurantService = restaurantService;
    }

    [HttpGet("orders")]
    public async Task<ActionResult<OrderPageDto>> GetOrders([FromQuery] StaffOrderFilterDto filter)
    {
        var restaurantId = await _restaurantService.GetEmployeeRestaurantIdAsync(GetUserId());
        if (restaurantId == null) return BadRequest("Менеджер не привязан к ресторану");

        return Ok(await _orderService.GetRestaurantOrdersAsync(restaurantId.Value, filter));
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value!);
}

[ApiController]
[Route("api/courier")]
[Authorize(Roles = "Courier")]
public class CourierController : ControllerBase
{
    private readonly OrderService _orderService;

    public CourierController(OrderService orderService) => _orderService = orderService;

    [HttpGet("orders")]
    public async Task<ActionResult<OrderPageDto>> GetAvailableOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
        => Ok(await _orderService.GetCourierAvailableOrdersAsync(page, pageSize));

    [HttpPost("orders/{orderId}/take")]
    public async Task<IActionResult> TakeOrder(Guid orderId)
    {
        var (success, error) = await _orderService.ChangeOrderStatusAsync(orderId, GetUserId(), OrderStatus.Delivery, "Courier");
        return success ? NoContent() : BadRequest(new { message = error });
    }

    [HttpPost("orders/{orderId}/deliver")]
    public async Task<IActionResult> DeliverOrder(Guid orderId)
    {
        var (success, error) = await _orderService.ChangeOrderStatusAsync(orderId, GetUserId(), OrderStatus.Delivered, "Courier");
        return success ? NoContent() : BadRequest(new { message = error });
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value!);
}
