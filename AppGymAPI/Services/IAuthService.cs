using AppGymAPI.DTOs;
using AppGymAPI.Models;

namespace AppGymAPI.Services;

public interface IAuthService
{
    Task<AuthResponseDto?> LoginUserAsync(LoginUserDto dto);
    Task<bool> RegisterUserAsync(RegisterUserDto dto);
    Task<AuthResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto dto);

}