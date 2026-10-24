using AppGymAPI.DTOs;
using AppGymAPI.Extensions;
using AppGymAPI.Models;
using AppGymAPI.Services;

namespace AppGymAPI.Endpoints;

public static class UserEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/users").RequireAuthorization();
        group.MapGet("{id:guid}", GetUserByIdAsync);
        group.MapGet("by-name/{name}", GetUsersByNameAsync);
        group.MapGet("by-role/{role}", GetUsersByRoleAsync);
        group.MapPut("{id:guid}", PutUserAsync)
            .RequireOwnerOrRole("id", Role.Admin);
        group.MapDelete("{id:guid}", DeleteUserAsync)
            .RequireRoles(Role.Admin);
    }

    private static async Task<IResult> GetUserByIdAsync(IUserService userService, Guid id,
        CancellationToken ct=default)
    {
        var result = await userService.SearchUserByIdAsync(id, ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> GetUsersByNameAsync(IUserService userService, string name,int? pageSize,
        int? pageNumber, CancellationToken ct = default)
    {
        int size = pageSize.GetValueOrDefault(10);
        int number = pageNumber.GetValueOrDefault(1);
        
        var result = await userService.SearchUsersByNameAsync(name,size,number,ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetUsersByRoleAsync(IUserService userService, Role role, int? pageSize,
        int? pageNumber, CancellationToken ct = default)
    {
        int size = pageSize.GetValueOrDefault(10);
        int number = pageNumber.GetValueOrDefault(1);
        
        var result = await userService.SearchUsersByRoleAsync(role,size,number,ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> PutUserAsync(IUserService userService, UserEditDto request,Guid id,
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