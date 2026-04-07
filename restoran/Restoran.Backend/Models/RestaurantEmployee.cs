namespace Restoran.Backend.Models;

public class RestaurantEmployee
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid RestaurantId { get; set; }
    public EmployeeRole Role { get; set; }

    public Restaurant Restaurant { get; set; } = null!;
}

public enum EmployeeRole
{
    Manager,
    Cook
}
