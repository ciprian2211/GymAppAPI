namespace AppGymAPI.Models;

public enum Role
{
    User,
    Trainer,
    Admin
};

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string ImageUrl { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Surname { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
    
    public float ReviewStars { get; set; } = 0.0f;

    public Role Role { get; set; } = Role.User;

    public List<Workout> Workouts { get; set; } = new();
}