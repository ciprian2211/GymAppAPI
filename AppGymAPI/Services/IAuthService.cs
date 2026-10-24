using AppGymAPI.DTOs;
using AppGymAPI.Models;

namespace AppGymAPI.Services;

public interface IAuthService
{
    Task<AuthResponseDto?> LoginUserAsync(LoginUserDto dto);
    Task<RegisterResultDto> RegisterUserAsync(RegisterUserDto dto, CancellationToken cancellationToken = default);
    Task<AuthResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto dto);
}