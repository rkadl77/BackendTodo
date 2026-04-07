using System.ComponentModel.DataAnnotations;
using Restoran.Backend.Models;

namespace Restoran.Backend.DTOs;

public class OrderDto
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeliveryTime { get; set; }
    public string DeliveryAddress { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public List<OrderDishDto> Dishes { get; set; } = new();
}

public class OrderDishDto
{
    public Guid DishId { get; set; }
    public string DishName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Price { get; set; }
}

public class CreateOrderDto
{
    [Required]
    public string DeliveryAddress { get; set; } = string.Empty;
    public DateTime? DeliveryTime { get; set; }
}

public class OrderPageDto
{
    public List<OrderDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class OrderFilterDto
{
    public Guid? OrderId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class StaffOrderFilterDto
{
    public List<OrderStatus>? Statuses { get; set; }
    public Guid? OrderId { get; set; }
    public OrderSortBy SortBy { get; set; } = OrderSortBy.CreatedAtDesc;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public enum OrderSortBy
{
    CreatedAtAsc,
    CreatedAtDesc,
    DeliveryTimeAsc,
    DeliveryTimeDesc
}
