using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restoran.Backend.DTOs;
using Restoran.Backend.Models;
using Restoran.Backend.Services;

namespace Restoran.Backend.Controllers;

[ApiController]
[Route("api/restaurants")]
public class RestaurantsController : ControllerBase
{
    private readonly RestaurantService _restaurantService;

    public RestaurantsController(RestaurantService restaurantService)
        => _restaurantService = restaurantService;

    [HttpGet]
    public async Task<ActionResult<RestaurantPageDto>> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
        => Ok(await _restaurantService.GetRestaurantsAsync(search, page, pageSize));

    [HttpGet("{id}")]
    public async Task<ActionResult<RestaurantDetailDto>> GetById(Guid id)
    {
        var result = await _restaurantService.GetByIdAsync(id);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<RestaurantDto>> Create([FromBody] CreateRestaurantDto dto)
        => Ok(await _restaurantService.CreateAsync(dto));

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<RestaurantDto>> Update(Guid id, [FromBody] UpdateRestaurantDto dto)
    {
        var result = await _restaurantService.UpdateAsync(id, dto);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _restaurantService.DeleteAsync(id);
        return success ? NoContent() : NotFound();
    }

    [HttpPost("{id}/menus")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<MenuDto>> CreateMenu(Guid id, [FromBody] CreateMenuDto dto)
    {
        var result = await _restaurantService.CreateMenuAsync(id, dto);
        return result == null ? NotFound("Ресторан не найден") : Ok(result);
    }

    [HttpPut("{id}/menus/{menuId}")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<MenuDto>> UpdateMenu(Guid id, Guid menuId, [FromBody] UpdateMenuDto dto)
    {
        var result = await _restaurantService.UpdateMenuAsync(id, menuId, dto);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id}/menus/{menuId}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> DeleteMenu(Guid id, Guid menuId)
    {
        var success = await _restaurantService.DeleteMenuAsync(id, menuId);
        return success ? NoContent() : NotFound();
    }

    [HttpGet("{id}/employees")]
    [Authorize]
    public async Task<ActionResult<List<EmployeeDto>>> GetEmployees(Guid id)
        => Ok(await _restaurantService.GetEmployeesAsync(id));

    [HttpPost("{id}/employees")]
    [Authorize]
    public async Task<IActionResult> AssignEmployee(Guid id, [FromQuery] Guid userId, [FromQuery] EmployeeRole role)
    {
        var success = await _restaurantService.AssignEmployeeAsync(id, userId, role);
        return success ? NoContent() : NotFound("Ресторан не найден");
    }

    [HttpDelete("{id}/employees/{userId}")]
    [Authorize]
    public async Task<IActionResult> RemoveEmployee(Guid id, Guid userId)
    {
        var success = await _restaurantService.RemoveEmployeeAsync(id, userId);
        return success ? NoContent() : NotFound();
    }
}
