namespace Restoran.Backend.Models;

public class Rating
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DishId { get; set; }
    public Guid UserId { get; set; }
    public int Score { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Dish Dish { get; set; } = null!;
}
