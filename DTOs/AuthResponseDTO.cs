namespace AppGymAPI.DTOs;

public class AuthResponseDTO
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}