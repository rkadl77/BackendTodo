using System.ComponentModel.DataAnnotations;

namespace Restoran.Backend.DTOs;

public class DishDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsVegetarian { get; set; }
    public double? Rating { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public Guid MenuId { get; set; }
    public Guid RestaurantId { get; set; }
}

public class DishPageDto
{
    public List<DishDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class CreateDishDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    [Required, Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsVegetarian { get; set; }
    [Required]
    public Guid CategoryId { get; set; }
    [Required]
    public Guid MenuId { get; set; }
}

public class UpdateDishDto
{
    [MaxLength(200)]
    public string? Name { get; set; }
    public string? Description { get; set; }
    [Range(0.01, double.MaxValue)]
    public decimal? Price { get; set; }
    public string? ImageUrl { get; set; }
    public bool? IsVegetarian { get; set; }
    public Guid? CategoryId { get; set; }
}

public class DishFilterDto
{
    public List<Guid>? CategoryIds { get; set; }
    public Guid? MenuId { get; set; }
    public bool? IsVegetarian { get; set; }
    public DishSortBy SortBy { get; set; } = DishSortBy.NameAsc;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public enum DishSortBy
{
    NameAsc,
    NameDesc,
    PriceAsc,
    PriceDesc,
    RatingAsc,
    RatingDesc
}

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CreateCategoryDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
