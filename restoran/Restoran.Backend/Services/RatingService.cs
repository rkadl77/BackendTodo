using Microsoft.EntityFrameworkCore;
using Restoran.Backend.Data;
using Restoran.Backend.DTOs;
using Restoran.Backend.Models;

namespace Restoran.Backend.Services;

public class RatingService
{
    private readonly BackendDbContext _db;

    public RatingService(BackendDbContext db) => _db = db;

    public async Task<(bool Success, string? Error)> SetRatingAsync(Guid userId, Guid dishId, int score)
    {
        var canRate = await _db.Orders
            .Where(o => o.UserId == userId && o.Status == OrderStatus.Delivered)
            .AnyAsync(o => o.OrderDishes.Any(od => od.DishId == dishId));

        if (!canRate)
            return (false, "Оценку можно поставить только для доставленного заказа с этим блюдом");

        var existing = await _db.Ratings.FirstOrDefaultAsync(r => r.DishId == dishId && r.UserId == userId);
        if (existing != null)
        {
            existing.Score = score;
            existing.CreatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.Ratings.Add(new Rating { DishId = dishId, UserId = userId, Score = score });
        }

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> DeleteRatingAsync(Guid userId, Guid dishId)
    {
        var rating = await _db.Ratings.FirstOrDefaultAsync(r => r.DishId == dishId && r.UserId == userId);
        if (rating == null) return false;
        _db.Ratings.Remove(rating);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<RatingDto?> GetRatingAsync(Guid dishId, Guid? userId)
    {
        if (!await _db.Dishes.AnyAsync(d => d.Id == dishId)) return null;

        var ratings = await _db.Ratings.Where(r => r.DishId == dishId).ToListAsync();
        int? myScore = null;

        if (userId.HasValue)
            myScore = ratings.FirstOrDefault(r => r.UserId == userId.Value)?.Score;

        return new RatingDto
        {
            DishId = dishId,
            AverageRating = ratings.Any() ? ratings.Average(r => r.Score) : 0,
            TotalRatings = ratings.Count,
            MyScore = myScore
        };
    }
}
