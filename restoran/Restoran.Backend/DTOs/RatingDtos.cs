using System.ComponentModel.DataAnnotations;

namespace Restoran.Backend.DTOs;

public class SetRatingDto
{
    [Required, Range(1, 10)]
    public int Score { get; set; }
}

public class RatingDto
{
    public Guid DishId { get; set; }
    public double AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public int? MyScore { get; set; }
}
