using AppGymAPI.DTOs;
using AppGymAPI.Services;

namespace AppGymAPI.Endpoints;

public static class AuthEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/auth/");
        group.MapPost("register", RegisterUser);
        group.MapPost("login", LoginUser);
        group.MapPost("refresh-token", RefreshToken);
    }

    private static async Task<IResult> RegisterUser(IAuthService authService, RegisterUserDto request, CancellationToken cancellationToken)
    {
        var result = await authService.RegisterUserAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            return Results.BadRequest(new { Errors = result.Errors });
        }

        return Results.Created();
    }

    private static async Task<IResult> LoginUser(IAuthService authService, LoginUserDto request)
    {
        var result = await authService.LoginUserAsync(request);
        if (result is null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> RefreshToken(IAuthService authService, RefreshTokenRequestDto request)
    {
        var result = await authService.RefreshTokenAsync(request);
        if (result is null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(result);
    }
}