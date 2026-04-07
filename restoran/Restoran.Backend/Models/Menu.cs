namespace Restoran.Backend.Models;

public class Menu
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RestaurantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Restaurant Restaurant { get; set; } = null!;
    public ICollection<Dish> Dishes { get; set; } = new List<Dish>();
}
