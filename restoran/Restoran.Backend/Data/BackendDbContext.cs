using Microsoft.EntityFrameworkCore;
using Restoran.Backend.Models;

namespace Restoran.Backend.Data;

public class BackendDbContext : DbContext
{
    public BackendDbContext(DbContextOptions<BackendDbContext> options) : base(options) { }

    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Dish> Dishes => Set<Dish>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDish> OrderDishes => Set<OrderDish>();
    public DbSet<DishInCart> Cart => Set<DishInCart>();
    public DbSet<RestaurantEmployee> RestaurantEmployees => Set<RestaurantEmployee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Dish>(b =>
        {
            b.Property(d => d.Price).HasColumnType("decimal(18,2)");
            b.HasOne(d => d.Restaurant).WithMany().HasForeignKey(d => d.RestaurantId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Order>(b =>
        {
            b.Property(o => o.TotalPrice).HasColumnType("decimal(18,2)");
            b.Property(o => o.Status).HasConversion<string>();
        });

        modelBuilder.Entity<OrderDish>(b =>
        {
            b.Property(od => od.Price).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Rating>(b =>
        {
            b.HasIndex(r => new { r.DishId, r.UserId }).IsUnique();
        });

        modelBuilder.Entity<RestaurantEmployee>(b =>
        {
            b.Property(e => e.Role).HasConversion<string>();
            b.HasIndex(e => new { e.UserId, e.RestaurantId }).IsUnique();
        });
    }
}
