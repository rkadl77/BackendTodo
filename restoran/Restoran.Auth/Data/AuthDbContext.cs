using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Restoran.Auth.Models;

namespace Restoran.Auth.Data;

public class AuthDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(b =>
        {
            b.Property(u => u.FullName).HasMaxLength(200).IsRequired();
            b.Property(u => u.Address).HasMaxLength(500);
        });

        builder.Entity<RefreshToken>(b =>
        {
            b.HasIndex(r => r.Token).IsUnique();
            b.HasIndex(r => r.UserId);
        });
    }
}
