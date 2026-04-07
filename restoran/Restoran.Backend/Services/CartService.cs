using Microsoft.EntityFrameworkCore;
using Restoran.Backend.Data;
using Restoran.Backend.DTOs;
using Restoran.Backend.Models;

namespace Restoran.Backend.Services;

public class CartService
{
    private readonly BackendDbContext _db;

    public CartService(BackendDbContext db) => _db = db;

    public async Task<CartDto> GetCartAsync(Guid userId)
    {
        var items = await _db.Cart
            .Include(c => c.Dish)
            .Where(c => c.UserId == userId)
            .ToListAsync();

        return new CartDto
        {
            Items = items.Select(c => new CartItemDto
            {
                DishId = c.DishId,
                DishName = c.Dish.Name,
                Price = c.Dish.Price,
                Count = c.Count,
                ImageUrl = c.Dish.ImageUrl,
                RestaurantId = c.RestaurantId
            }).ToList(),
            TotalPrice = items.Sum(c => c.Dish.Price * c.Count),
            RestaurantId = items.Select(c => (Guid?)c.RestaurantId).FirstOrDefault()
        };
    }

    public async Task<(bool Success, string? Error)> AddDishAsync(Guid userId, Guid dishId)
    {
        var dish = await _db.Dishes.FindAsync(dishId);
        if (dish == null) return (false, "Блюдо не найдено");

        var existingCartItems = await _db.Cart.Where(c => c.UserId == userId).ToListAsync();
        if (existingCartItems.Any() && existingCartItems.First().RestaurantId != dish.RestaurantId)
            return (false, "В корзине уже есть блюда из другого ресторана");

        var cartItem = existingCartItems.FirstOrDefault(c => c.DishId == dishId);
        if (cartItem != null)
        {
            cartItem.Count++;
        }
        else
        {
            _db.Cart.Add(new DishInCart
            {
                UserId = userId,
                DishId = dishId,
                RestaurantId = dish.RestaurantId,
                Count = 1
            });
        }

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> RemoveDishAsync(Guid userId, Guid dishId)
    {
        var item = await _db.Cart.FirstOrDefaultAsync(c => c.UserId == userId && c.DishId == dishId);
        if (item == null) return false;
        _db.Cart.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IncreaseDishAsync(Guid userId, Guid dishId)
    {
        var item = await _db.Cart.FirstOrDefaultAsync(c => c.UserId == userId && c.DishId == dishId);
        if (item == null) return false;
        item.Count++;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DecreaseDishAsync(Guid userId, Guid dishId)
    {
        var item = await _db.Cart.FirstOrDefaultAsync(c => c.UserId == userId && c.DishId == dishId);
        if (item == null) return false;

        if (item.Count <= 1)
        {
            _db.Cart.Remove(item);
        }
        else
        {
            item.Count--;
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task ClearCartAsync(Guid userId)
    {
        var items = await _db.Cart.Where(c => c.UserId == userId).ToListAsync();
        _db.Cart.RemoveRange(items);
        await _db.SaveChangesAsync();
    }
}
