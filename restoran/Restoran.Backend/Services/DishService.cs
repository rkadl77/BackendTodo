using Microsoft.EntityFrameworkCore;
using Restoran.Backend.Data;
using Restoran.Backend.DTOs;
using Restoran.Backend.Models;

namespace Restoran.Backend.Services;

public class DishService
{
    private readonly BackendDbContext _db;

    public DishService(BackendDbContext db) => _db = db;

    public async Task<DishPageDto> GetDishesAsync(Guid restaurantId, DishFilterDto filter)
    {
        var query = _db.Dishes
            .Include(d => d.Category)
            .Include(d => d.Ratings)
            .Where(d => d.RestaurantId == restaurantId);

        if (filter.CategoryIds?.Any() == true)
            query = query.Where(d => filter.CategoryIds.Contains(d.CategoryId));

        if (filter.MenuId.HasValue)
            query = query.Where(d => d.MenuId == filter.MenuId);

        if (filter.IsVegetarian.HasValue)
            query = query.Where(d => d.IsVegetarian == filter.IsVegetarian);

        var withRating = query.Select(d => new
        {
            Dish = d,
            AvgRating = d.Ratings.Any() ? d.Ratings.Average(r => (double)r.Score) : 0.0
        });

        withRating = filter.SortBy switch
        {
            DishSortBy.NameAsc => withRating.OrderBy(x => x.Dish.Name),
            DishSortBy.NameDesc => withRating.OrderByDescending(x => x.Dish.Name),
            DishSortBy.PriceAsc => withRating.OrderBy(x => x.Dish.Price),
            DishSortBy.PriceDesc => withRating.OrderByDescending(x => x.Dish.Price),
            DishSortBy.RatingAsc => withRating.OrderBy(x => x.AvgRating),
            DishSortBy.RatingDesc => withRating.OrderByDescending(x => x.AvgRating),
            _ => withRating.OrderBy(x => x.Dish.Name)
        };

        var total = await withRating.CountAsync();
        var items = await withRating
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new DishPageDto
        {
            Items = items.Select(x => MapToDto(x.Dish, x.AvgRating)).ToList(),
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<DishDto?> GetByIdAsync(Guid dishId)
    {
        var dish = await _db.Dishes
            .Include(d => d.Category)
            .Include(d => d.Ratings)
            .FirstOrDefaultAsync(d => d.Id == dishId);

        if (dish == null) return null;
        var avg = dish.Ratings.Any() ? dish.Ratings.Average(r => (double)r.Score) : 0.0;
        return MapToDto(dish, avg);
    }

    public async Task<DishDto?> CreateAsync(Guid restaurantId, CreateDishDto dto)
    {
        var menu = await _db.Menus.FirstOrDefaultAsync(m => m.Id == dto.MenuId && m.RestaurantId == restaurantId);
        if (menu == null) return null;

        if (!await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId)) return null;

        var dish = new Dish
        {
            RestaurantId = restaurantId,
            MenuId = dto.MenuId,
            CategoryId = dto.CategoryId,
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            ImageUrl = dto.ImageUrl,
            IsVegetarian = dto.IsVegetarian
        };

        _db.Dishes.Add(dish);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(dish.Id);
    }

    public async Task<DishDto?> UpdateAsync(Guid dishId, Guid restaurantId, UpdateDishDto dto)
    {
        var dish = await _db.Dishes.FirstOrDefaultAsync(d => d.Id == dishId && d.RestaurantId == restaurantId);
        if (dish == null) return null;

        if (dto.Name != null) dish.Name = dto.Name;
        if (dto.Description != null) dish.Description = dto.Description;
        if (dto.Price.HasValue) dish.Price = dto.Price.Value;
        if (dto.ImageUrl != null) dish.ImageUrl = dto.ImageUrl;
        if (dto.IsVegetarian.HasValue) dish.IsVegetarian = dto.IsVegetarian.Value;
        if (dto.CategoryId.HasValue) dish.CategoryId = dto.CategoryId.Value;

        await _db.SaveChangesAsync();
        return await GetByIdAsync(dishId);
    }

    public async Task<bool> DeleteAsync(Guid dishId, Guid restaurantId)
    {
        var dish = await _db.Dishes.FirstOrDefaultAsync(d => d.Id == dishId && d.RestaurantId == restaurantId);
        if (dish == null) return false;
        _db.Dishes.Remove(dish);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        return await _db.Categories
            .Select(c => new CategoryDto { Id = c.Id, Name = c.Name })
            .ToListAsync();
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto)
    {
        var cat = new Category { Name = dto.Name };
        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();
        return new CategoryDto { Id = cat.Id, Name = cat.Name };
    }

    private static DishDto MapToDto(Dish dish, double avgRating) => new()
    {
        Id = dish.Id,
        Name = dish.Name,
        Description = dish.Description,
        Price = dish.Price,
        ImageUrl = dish.ImageUrl,
        IsVegetarian = dish.IsVegetarian,
        Rating = avgRating,
        CategoryId = dish.CategoryId,
        CategoryName = dish.Category?.Name ?? string.Empty,
        MenuId = dish.MenuId,
        RestaurantId = dish.RestaurantId
    };
}
