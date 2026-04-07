using System.ComponentModel.DataAnnotations;
using Restoran.Backend.Models;

namespace Restoran.Backend.DTOs;

public class RestaurantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
}

public class RestaurantDetailDto : RestaurantDto
{
    public List<MenuDto> Menus { get; set; } = new();
}

public class CreateRestaurantDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    [Required]
    public string Address { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
}

public class UpdateRestaurantDto
{
    [MaxLength(200)]
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? LogoUrl { get; set; }
}

public class RestaurantPageDto
{
    public List<RestaurantDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class MenuDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CreateMenuDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateMenuDto
{
    [MaxLength(200)]
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class EmployeeDto
{
    public Guid UserId { get; set; }
    public EmployeeRole Role { get; set; }
}
