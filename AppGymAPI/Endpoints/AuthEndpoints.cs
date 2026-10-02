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

    private static async Task<IResult> RegisterUser(IAuthService authService,RegisterUserDTO request)
    {
        var result = await authService.RegisterUserAsync(request);
        if (!result)
        {
            return Results.BadRequest(new { Error = "Something went wrong. Try again." });
        }

        return Results.Created();
    }

    private static async Task<IResult> LoginUser(IAuthService authService, LoginUserDTO request)
    {
        var result = await authService.LoginUserAsync(request);
        if (result is null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> RefreshToken(IAuthService authService, RefreshTokenRequestDTO request)
    {
        var result = await authService.RefreshTokenAsync(request);
        if (result is null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(result);
    }
}