namespace Restoran.Backend.Models;

public class Dish
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MenuId { get; set; }
    public Guid RestaurantId { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsVegetarian { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Menu Menu { get; set; } = null!;
    public Restaurant Restaurant { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    public ICollection<DishInCart> CartItems { get; set; } = new List<DishInCart>();
    public ICollection<OrderDish> OrderDishes { get; set; } = new List<OrderDish>();
}
