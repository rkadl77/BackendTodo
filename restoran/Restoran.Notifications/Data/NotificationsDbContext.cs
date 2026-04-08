using Microsoft.EntityFrameworkCore;
using Restoran.Notifications.Models;

namespace Restoran.Notifications.Data;

public class NotificationsDbContext : DbContext
{
    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : base(options) { }

    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Notification>(b =>
        {
            b.Property(n => n.Status).HasConversion<string>();
            b.Property(n => n.Message).HasMaxLength(1000);
            b.HasIndex(n => n.UserId);
        });
    }
}
