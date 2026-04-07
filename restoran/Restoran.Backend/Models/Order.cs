namespace Restoran.Backend.Models;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid RestaurantId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Created;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveryTime { get; set; }
    public string DeliveryAddress { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public Guid? CookId { get; set; }
    public Guid? CourierId { get; set; }

    public Restaurant Restaurant { get; set; } = null!;
    public ICollection<OrderDish> OrderDishes { get; set; } = new List<OrderDish>();
}

public enum OrderStatus
{
    Created,
    Kitchen,
    Packaging,
    Delivery,
    Delivered,
    Cancelled
}
