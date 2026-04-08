using Microsoft.AspNetCore.Identity;

namespace Restoran.Auth.Models;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public Gender? Gender { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsBanned { get; set; } = false;
}

public enum Gender
{
    Male,
    Female
}
