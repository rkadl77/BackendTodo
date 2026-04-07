using Microsoft.EntityFrameworkCore;
using Restoran.Backend.Data;
using Restoran.Backend.DTOs;
using Restoran.Backend.Models;

namespace Restoran.Backend.Services;

public class RestaurantService
{
    private readonly BackendDbContext _db;

    public RestaurantService(BackendDbContext db) => _db = db;

    public async Task<RestaurantPageDto> GetRestaurantsAsync(string? search, int page, int pageSize)
    {
        var query = _db.Restaurants.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r => r.Name.ToLower().Contains(search.ToLower()));

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(r => r.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RestaurantDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Address = r.Address,
                LogoUrl = r.LogoUrl
            })
            .ToListAsync();

        return new RestaurantPageDto { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<RestaurantDetailDto?> GetByIdAsync(Guid id)
    {
        var r = await _db.Restaurants
            .Include(r => r.Menus)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (r == null) return null;

        return new RestaurantDetailDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            Address = r.Address,
            LogoUrl = r.LogoUrl,
            Menus = r.Menus.Select(m => new MenuDto { Id = m.Id, Name = m.Name, Description = m.Description }).ToList()
        };
    }

    public async Task<RestaurantDto> CreateAsync(CreateRestaurantDto dto)
    {
        var restaurant = new Restaurant
        {
            Name = dto.Name,
            Description = dto.Description,
            Address = dto.Address,
            LogoUrl = dto.LogoUrl
        };
        _db.Restaurants.Add(restaurant);
        await _db.SaveChangesAsync();
        return new RestaurantDto { Id = restaurant.Id, Name = restaurant.Name, Description = restaurant.Description, Address = restaurant.Address, LogoUrl = restaurant.LogoUrl };
    }

    public async Task<RestaurantDto?> UpdateAsync(Guid id, UpdateRestaurantDto dto)
    {
        var restaurant = await _db.Restaurants.FindAsync(id);
        if (restaurant == null) return null;

        if (dto.Name != null) restaurant.Name = dto.Name;
        if (dto.Description != null) restaurant.Description = dto.Description;
        if (dto.Address != null) restaurant.Address = dto.Address;
        if (dto.LogoUrl != null) restaurant.LogoUrl = dto.LogoUrl;

        await _db.SaveChangesAsync();
        return new RestaurantDto { Id = restaurant.Id, Name = restaurant.Name, Description = restaurant.Description, Address = restaurant.Address, LogoUrl = restaurant.LogoUrl };
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var restaurant = await _db.Restaurants.FindAsync(id);
        if (restaurant == null) return false;
        _db.Restaurants.Remove(restaurant);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<MenuDto?> CreateMenuAsync(Guid restaurantId, CreateMenuDto dto)
    {
        if (!await _db.Restaurants.AnyAsync(r => r.Id == restaurantId)) return null;

        var menu = new Menu { RestaurantId = restaurantId, Name = dto.Name, Description = dto.Description };
        _db.Menus.Add(menu);
        await _db.SaveChangesAsync();
        return new MenuDto { Id = menu.Id, Name = menu.Name, Description = menu.Description };
    }

    public async Task<MenuDto?> UpdateMenuAsync(Guid restaurantId, Guid menuId, UpdateMenuDto dto)
    {
        var menu = await _db.Menus.FirstOrDefaultAsync(m => m.Id == menuId && m.RestaurantId == restaurantId);
        if (menu == null) return null;

        if (dto.Name != null) menu.Name = dto.Name;
        if (dto.Description != null) menu.Description = dto.Description;

        await _db.SaveChangesAsync();
        return new MenuDto { Id = menu.Id, Name = menu.Name, Description = menu.Description };
    }

    public async Task<bool> DeleteMenuAsync(Guid restaurantId, Guid menuId)
    {
        var menu = await _db.Menus.FirstOrDefaultAsync(m => m.Id == menuId && m.RestaurantId == restaurantId);
        if (menu == null) return false;
        _db.Menus.Remove(menu);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<EmployeeDto>> GetEmployeesAsync(Guid restaurantId)
    {
        return await _db.RestaurantEmployees
            .Where(e => e.RestaurantId == restaurantId)
            .Select(e => new EmployeeDto { UserId = e.UserId, Role = e.Role })
            .ToListAsync();
    }

    public async Task<bool> AssignEmployeeAsync(Guid restaurantId, Guid userId, EmployeeRole role)
    {
        if (!await _db.Restaurants.AnyAsync(r => r.Id == restaurantId)) return false;

        var existing = await _db.RestaurantEmployees
            .FirstOrDefaultAsync(e => e.UserId == userId && e.RestaurantId == restaurantId);

        if (existing != null)
        {
            existing.Role = role;
        }
        else
        {
            _db.RestaurantEmployees.Add(new RestaurantEmployee
            {
                UserId = userId,
                RestaurantId = restaurantId,
                Role = role
            });
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveEmployeeAsync(Guid restaurantId, Guid userId)
    {
        var emp = await _db.RestaurantEmployees
            .FirstOrDefaultAsync(e => e.UserId == userId && e.RestaurantId == restaurantId);
        if (emp == null) return false;
        _db.RestaurantEmployees.Remove(emp);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<Guid?> GetEmployeeRestaurantIdAsync(Guid userId)
    {
        var emp = await _db.RestaurantEmployees.FirstOrDefaultAsync(e => e.UserId == userId);
        return emp?.RestaurantId;
    }
}
