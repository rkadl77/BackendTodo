using System.ComponentModel.DataAnnotations;

namespace Restoran.Backend.DTOs;

public class CartDto
{
    public List<CartItemDto> Items { get; set; } = new();
    public decimal TotalPrice { get; set; }
    public Guid? RestaurantId { get; set; }
}

public class CartItemDto
{
    public Guid DishId { get; set; }
    public string DishName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Count { get; set; }
    public string? ImageUrl { get; set; }
    public Guid RestaurantId { get; set; }
}

public class AddToCartDto
{
    [Required]
    public Guid DishId { get; set; }
}
