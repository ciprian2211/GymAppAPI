using Microsoft.AspNetCore.Identity;

namespace AppGymAPI.Models;

public enum Role
{
    User,
    Trainer,
    Admin
};

public class User : IdentityUser<Guid>
{
    
    public string ImageUrl { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Surname { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    
    public float ReviewStars { get; set; } = 0.0f;

    public Role Role { get; set; } = Role.User;

    public List<Workout> Workouts { get; set; } = new();
    
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiryTime { get; set; }
}