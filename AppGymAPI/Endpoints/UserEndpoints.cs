using AppGymAPI.DTOs;
using AppGymAPI.Models;
using AppGymAPI.Services;

namespace AppGymAPI.Endpoints;

public static class UserEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/users");
        group.MapGet("{id:guid}", GetUserByIdAsync);
        group.MapGet("by-name/{name}", GetUsersByNameAsync);
        group.MapGet("by-role/{role}", GetUsersByRoleAsync);
        group.MapPut("{id:guid}", PutUserAsync);
        group.MapDelete("{id:guid}", DeleteUserAsync);
    }

    private static async Task<IResult> GetUserByIdAsync(IUserService userService, Guid id,
        CancellationToken ct=default)
    {
        var result = await userService.SearchUserByIdAsync(id, ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> GetUsersByNameAsync(IUserService userService, string name,
        CancellationToken ct = default)
    {
        var result = await userService.SearchUsersByNameAsync(name, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetUsersByRoleAsync(IUserService userService, Role role,
        CancellationToken ct = default)
    {
        var result = await userService.SearchUsersByRoleAsync(role, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> PutUserAsync(IUserService userService, UserEditDTO request,Guid id,
        CancellationToken ct = default)
    {
        var result = await userService.EditUserAsync(id,request, ct);
        return result is null ? Results.NotFound() : Results.NoContent();
    }

    private static async Task<IResult> DeleteUserAsync(IUserService userService, Guid id,
        CancellationToken ct = default)
    {
        var result = await userService.DeleteUserByIdAsync(id,ct);
        
        return result ? Results.NoContent() : Results.NotFound();
    }
}