namespace AppGymAPI.DTOs;

public record UserListDTO
{
    public string Name { get; set; } = string.Empty;

    public string Surname { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public float ReviewStars { get; set; } = 0.0f;
};