using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restoran.Backend.DTOs;
using Restoran.Backend.Services;

namespace Restoran.Backend.Controllers;

[ApiController]
[Route("api")]
public class DishesController : ControllerBase
{
    private readonly DishService _dishService;
    private readonly RatingService _ratingService;
    private readonly RestaurantService _restaurantService;

    public DishesController(DishService dishService, RatingService ratingService, RestaurantService restaurantService)
    {
        _dishService = dishService;
        _ratingService = ratingService;
        _restaurantService = restaurantService;
    }

    [HttpGet("restaurants/{restaurantId}/dishes")]
    public async Task<ActionResult<DishPageDto>> GetDishes(Guid restaurantId, [FromQuery] DishFilterDto filter)
        => Ok(await _dishService.GetDishesAsync(restaurantId, filter));

    [HttpGet("dishes/{id}")]
    public async Task<ActionResult<DishDto>> GetById(Guid id)
    {
        var result = await _dishService.GetByIdAsync(id);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("restaurants/{restaurantId}/dishes")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<DishDto>> Create(Guid restaurantId, [FromBody] CreateDishDto dto)
    {
        var restaurantIdForUser = await _restaurantService.GetEmployeeRestaurantIdAsync(GetUserId());
        if (restaurantIdForUser != restaurantId)
            return Forbid();

        var result = await _dishService.CreateAsync(restaurantId, dto);
        return result == null ? BadRequest("Неверное меню или категория") : Ok(result);
    }

    [HttpPut("restaurants/{restaurantId}/dishes/{dishId}")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<DishDto>> Update(Guid restaurantId, Guid dishId, [FromBody] UpdateDishDto dto)
    {
        var restaurantIdForUser = await _restaurantService.GetEmployeeRestaurantIdAsync(GetUserId());
        if (restaurantIdForUser != restaurantId)
            return Forbid();

        var result = await _dishService.UpdateAsync(dishId, restaurantId, dto);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpDelete("restaurants/{restaurantId}/dishes/{dishId}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Delete(Guid restaurantId, Guid dishId)
    {
        var restaurantIdForUser = await _restaurantService.GetEmployeeRestaurantIdAsync(GetUserId());
        if (restaurantIdForUser != restaurantId)
            return Forbid();

        var success = await _dishService.DeleteAsync(dishId, restaurantId);
        return success ? NoContent() : NotFound();
    }

    [HttpGet("categories")]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories()
        => Ok(await _dishService.GetCategoriesAsync());

    [HttpPost("categories")]
    [Authorize]
    public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CreateCategoryDto dto)
        => Ok(await _dishService.CreateCategoryAsync(dto));

    [HttpGet("dishes/{dishId}/rating")]
    public async Task<ActionResult<RatingDto>> GetRating(Guid dishId)
    {
        var userId = User.Identity?.IsAuthenticated == true ? GetUserId() : (Guid?)null;
        var result = await _ratingService.GetRatingAsync(dishId, userId);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("dishes/{dishId}/rating")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> SetRating(Guid dishId, [FromBody] SetRatingDto dto)
    {
        var (success, error) = await _ratingService.SetRatingAsync(GetUserId(), dishId, dto.Score);
        return success ? NoContent() : BadRequest(new { message = error });
    }

    [HttpDelete("dishes/{dishId}/rating")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> DeleteRating(Guid dishId)
    {
        var success = await _ratingService.DeleteRatingAsync(GetUserId(), dishId);
        return success ? NoContent() : NotFound();
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? throw new InvalidOperationException());
}
