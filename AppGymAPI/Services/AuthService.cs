using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AppGymAPI.DTOs;
using AppGymAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace AppGymAPI.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    
    public AuthService(UserManager<User> userManager, IConfiguration configuration, ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }
    
    public async Task<AuthResponseDTO?> LoginUserAsync(LoginUserDTO dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
        {
            _logger.LogWarning("Login failed: User with email {Email} not found", dto.Email);
            return null;
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!isPasswordValid)
        {
            _logger.LogWarning("Login failed: Invalid credentials for email {Email}", dto.Email);
            return null;
        }

        var jwtToken = GenerateToken(user);

        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(30);
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("User {Email} logged in successfully", dto.Email);

        return new AuthResponseDTO
        {
            Token = jwtToken,
            RefreshToken = refreshToken
        };
    }

    public async Task<bool> RegisterUserAsync(RegisterUserDTO dto)
    {
        string imageUrl = string.Empty;
        if (dto.ProfileImage != null && dto.ProfileImage.Length > 0)
        {
            imageUrl = "poza pentru ca da";
        }

        var newUser = new User
        {
            Email = dto.Email,
            UserName = dto.Email,
            Name = dto.Name,
            Surname = dto.Surname,
            ImageUrl = imageUrl,
            Role = Role.User
        };

        var result = await _userManager.CreateAsync(newUser, dto.Password);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} registered successfully", dto.Email);
            return true;
        }

        foreach (var error in result.Errors)
        {
            _logger.LogError("Registration error for {Email}: {Code} - {Description}", dto.Email, error.Code, error.Description);
        }
        return false;
    }

    public async Task<AuthResponseDTO?> RefreshTokenAsync(RefreshTokenRequestDTO dto)
    {
        var principal = GetPrincipalFromExpiredToken(dto.Token);
        if (principal == null)
        {
            _logger.LogWarning("Refresh token failed: Invalid expired token provided");
            return null;
        }

        var email = principal.FindFirst(ClaimTypes.Email)?.Value 
                    ?? principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
        if (email == null)
        {
            _logger.LogWarning("Refresh token failed: Email claim missing from token");
            return null;
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("Refresh token failed: User {Email} not found", email);
            return null;
        }

        if (user.RefreshToken != dto.RefreshToken)
        {
            _logger.LogWarning("Refresh token failed: Mismatched refresh token for user {Email}", email);
            return null;
        }

        if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            _logger.LogWarning("Refresh token failed: Refresh token expired for user {Email}", email);
            return null;
        }

        var newJwtToken = GenerateToken(user);
        var newRefreshToken = GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(30);
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Tokens refreshed successfully for user {Email}", email);

        return new AuthResponseDTO
        {
            Token = newJwtToken,
            RefreshToken = newRefreshToken
        };
    }

    private string GenerateToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub,user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email,user.Email??""),
            new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),
            new Claim(ClaimTypes.Role,user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken
        (
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires:DateTime.UtcNow.AddMinutes(15),
            signingCredentials:creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParams = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidIssuer = _configuration["JwtSettings:Issuer"],
            ValidAudience = _configuration["JwtSettings:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]!)),
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParams, out SecurityToken securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }
}