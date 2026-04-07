namespace Restoran.Backend.Models;

public class Restaurant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Menu> Menus { get; set; } = new List<Menu>();
    public ICollection<RestaurantEmployee> Employees { get; set; } = new List<RestaurantEmployee>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
