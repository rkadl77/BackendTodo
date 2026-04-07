using Microsoft.EntityFrameworkCore;
using Restoran.Backend.Data;
using Restoran.Backend.DTOs;
using Restoran.Backend.Messaging;
using Restoran.Backend.Models;

namespace Restoran.Backend.Services;

public class OrderService
{
    private readonly BackendDbContext _db;
    private readonly CartService _cartService;
    private readonly RabbitMqPublisher _publisher;

    public OrderService(BackendDbContext db, CartService cartService, RabbitMqPublisher publisher)
    {
        _db = db;
        _cartService = cartService;
        _publisher = publisher;
    }

    public async Task<(OrderDto? Order, string? Error)> CreateOrderAsync(Guid userId, CreateOrderDto dto)
    {
        var cartItems = await _db.Cart
            .Include(c => c.Dish)
            .Where(c => c.UserId == userId)
            .ToListAsync();

        if (!cartItems.Any())
            return (null, "Корзина пуста");

        var restaurantId = cartItems.First().RestaurantId;
        if (cartItems.Any(c => c.RestaurantId != restaurantId))
            return (null, "В корзине блюда из разных ресторанов");

        var order = new Order
        {
            UserId = userId,
            RestaurantId = restaurantId,
            DeliveryAddress = dto.DeliveryAddress,
            DeliveryTime = dto.DeliveryTime,
            Status = OrderStatus.Created,
            TotalPrice = cartItems.Sum(c => c.Dish.Price * c.Count)
        };

        _db.Orders.Add(order);

        foreach (var item in cartItems)
        {
            _db.OrderDishes.Add(new OrderDish
            {
                OrderId = order.Id,
                DishId = item.DishId,
                Count = item.Count,
                Price = item.Dish.Price
            });
        }

        await _db.SaveChangesAsync();
        await _cartService.ClearCartAsync(userId);

        await _publisher.PublishOrderStatusChangedAsync(order.Id, userId, order.Status);

        return (await GetOrderDtoAsync(order.Id), null);
    }

    public async Task<OrderPageDto> GetOrderHistoryAsync(Guid userId, OrderFilterDto filter)
    {
        var query = _db.Orders.Where(o => o.UserId == userId);

        if (filter.OrderId.HasValue)
            query = query.Where(o => o.Id == filter.OrderId);

        if (filter.DateFrom.HasValue)
            query = query.Where(o => o.CreatedAt >= filter.DateFrom);

        if (filter.DateTo.HasValue)
            query = query.Where(o => o.CreatedAt <= filter.DateTo);

        var total = await query.CountAsync();
        var orders = await query
            .Include(o => o.Restaurant)
            .Include(o => o.OrderDishes).ThenInclude(od => od.Dish)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new OrderPageDto
        {
            Items = orders.Select(MapToDto).ToList(),
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<OrderDto?> GetCurrentOrderAsync(Guid userId)
    {
        var order = await _db.Orders
            .Include(o => o.Restaurant)
            .Include(o => o.OrderDishes).ThenInclude(od => od.Dish)
            .Where(o => o.UserId == userId && o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        return order == null ? null : MapToDto(order);
    }

    public async Task<(bool Success, string? Error)> CancelOrderAsync(Guid orderId, Guid userId)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
        if (order == null) return (false, "Заказ не найден");
        if (order.Status != OrderStatus.Created)
            return (false, "Заказ можно отменить только в статусе Created");

        order.Status = OrderStatus.Cancelled;
        await _db.SaveChangesAsync();
        await _publisher.PublishOrderStatusChangedAsync(order.Id, userId, order.Status);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RepeatOrderAsync(Guid orderId, Guid userId)
    {
        var order = await _db.Orders
            .Include(o => o.OrderDishes).ThenInclude(od => od.Dish)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        if (order == null) return (false, "Заказ не найден");

        await _cartService.ClearCartAsync(userId);
        foreach (var od in order.OrderDishes)
        {
            for (int i = 0; i < od.Count; i++)
                await _cartService.AddDishAsync(userId, od.DishId);
        }

        return (true, null);
    }

    public async Task<OrderPageDto> GetRestaurantOrdersAsync(Guid restaurantId, StaffOrderFilterDto filter)
    {
        var query = _db.Orders
            .Include(o => o.Restaurant)
            .Include(o => o.OrderDishes).ThenInclude(od => od.Dish)
            .Where(o => o.RestaurantId == restaurantId);

        if (filter.Statuses?.Any() == true)
            query = query.Where(o => filter.Statuses.Contains(o.Status));

        if (filter.OrderId.HasValue)
            query = query.Where(o => o.Id == filter.OrderId);

        query = filter.SortBy switch
        {
            OrderSortBy.CreatedAtAsc => query.OrderBy(o => o.CreatedAt),
            OrderSortBy.CreatedAtDesc => query.OrderByDescending(o => o.CreatedAt),
            OrderSortBy.DeliveryTimeAsc => query.OrderBy(o => o.DeliveryTime),
            OrderSortBy.DeliveryTimeDesc => query.OrderByDescending(o => o.DeliveryTime),
            _ => query.OrderByDescending(o => o.CreatedAt)
        };

        var total = await query.CountAsync();
        var orders = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new OrderPageDto { Items = orders.Select(MapToDto).ToList(), TotalCount = total, Page = filter.Page, PageSize = filter.PageSize };
    }

    public async Task<(bool Success, string? Error)> ChangeOrderStatusAsync(
        Guid orderId, Guid actorId, OrderStatus newStatus, string actorRole)
    {
        var order = await _db.Orders.FindAsync(orderId);
        if (order == null) return (false, "Заказ не найден");

        var allowed = (actorRole, order.Status, newStatus) switch
        {
            ("Cook", OrderStatus.Created, OrderStatus.Kitchen) => true,
            ("Cook", OrderStatus.Kitchen, OrderStatus.Packaging) => true,
            ("Courier", OrderStatus.Packaging, OrderStatus.Delivery) => true,
            ("Courier", OrderStatus.Delivery, OrderStatus.Delivered) => true,
            _ => false
        };

        if (!allowed)
            return (false, $"Нельзя перевести заказ из {order.Status} в {newStatus} для роли {actorRole}");

        if (actorRole == "Cook") order.CookId = actorId;
        if (actorRole == "Courier") order.CourierId = actorId;

        order.Status = newStatus;
        await _db.SaveChangesAsync();
        await _publisher.PublishOrderStatusChangedAsync(order.Id, order.UserId, order.Status);
        return (true, null);
    }

    public async Task<OrderPageDto> GetCourierAvailableOrdersAsync(int page, int pageSize)
    {
        var query = _db.Orders
            .Include(o => o.Restaurant)
            .Include(o => o.OrderDishes).ThenInclude(od => od.Dish)
            .Where(o => o.Status == OrderStatus.Packaging && o.CourierId == null);

        var total = await query.CountAsync();
        var orders = await query.OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new OrderPageDto { Items = orders.Select(MapToDto).ToList(), TotalCount = total, Page = page, PageSize = pageSize };
    }

    private async Task<OrderDto?> GetOrderDtoAsync(Guid orderId)
    {
        var order = await _db.Orders
            .Include(o => o.Restaurant)
            .Include(o => o.OrderDishes).ThenInclude(od => od.Dish)
            .FirstOrDefaultAsync(o => o.Id == orderId);
        return order == null ? null : MapToDto(order);
    }

    private static OrderDto MapToDto(Order order) => new()
    {
        Id = order.Id,
        RestaurantId = order.RestaurantId,
        RestaurantName = order.Restaurant?.Name ?? string.Empty,
        Status = order.Status,
        CreatedAt = order.CreatedAt,
        DeliveryTime = order.DeliveryTime,
        DeliveryAddress = order.DeliveryAddress,
        TotalPrice = order.TotalPrice,
        Dishes = order.OrderDishes.Select(od => new OrderDishDto
        {
            DishId = od.DishId,
            DishName = od.Dish?.Name ?? string.Empty,
            Count = od.Count,
            Price = od.Price
        }).ToList()
    };
}
