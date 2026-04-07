namespace Restoran.Backend.Models;

public class OrderDish
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid DishId { get; set; }
    public int Count { get; set; }
    public decimal Price { get; set; }

    public Order Order { get; set; } = null!;
    public Dish Dish { get; set; } = null!;
}
