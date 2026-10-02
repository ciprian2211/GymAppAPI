using AppGymAPI.DTOs;
using AppGymAPI.Models;

namespace AppGymAPI.Services;

public interface IAuthService
{
    Task<AuthResponseDTO?> LoginUserAsync(LoginUserDTO dto);
    Task<bool> RegisterUserAsync(RegisterUserDTO dto);
    Task<AuthResponseDTO?> RefreshTokenAsync(RefreshTokenRequestDTO dto);

}