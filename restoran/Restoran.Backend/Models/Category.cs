namespace Restoran.Backend.Models;

public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;

    public ICollection<Dish> Dishes { get; set; } = new List<Dish>();
}
