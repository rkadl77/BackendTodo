namespace Restoran.Backend.Models;

public class DishInCart
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid DishId { get; set; }
    public Guid RestaurantId { get; set; }
    public int Count { get; set; } = 1;

    public Dish Dish { get; set; } = null!;
}
