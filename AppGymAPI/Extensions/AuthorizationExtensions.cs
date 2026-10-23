using System.Security.Claims;
using AppGymAPI.Models;
using Microsoft.AspNetCore.Http;

namespace AppGymAPI.Extensions;

public static class AuthorizationExtensions
{
    public static RouteHandlerBuilder RequireRoles(this RouteHandlerBuilder builder, params Role[] roles)
    {
        var roleNames = roles.Select(r => r.ToString()).ToArray();
        return builder.RequireAuthorization(p => p.RequireRole(roleNames));
    }

    public static RouteGroupBuilder RequireRoles(this RouteGroupBuilder builder, params Role[] roles)
    {
        var roleNames = roles.Select(r => r.ToString()).ToArray();
        return builder.RequireAuthorization(p => p.RequireRole(roleNames));
    }

    public static RouteHandlerBuilder RequireOwnerOrRole(
        this RouteHandlerBuilder builder,
        string routeParamName = "id",
        Role bypassRole = Role.Admin)
    {
        return builder
            .RequireAuthorization()
            .AddEndpointFilter((context, next) => ExecuteOwnerOrRoleFilterAsync(context, next, routeParamName, bypassRole));
    }

    public static async ValueTask<object?> ExecuteOwnerOrRoleFilterAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next,
        string routeParamName = "id",
        Role bypassRole = Role.Admin)
    {
        var user = context.HttpContext.User;

        if (user.IsInRole(bypassRole.ToString()))
        {
            return await next(context);
        }

        var routeId = context.HttpContext.GetRouteValue(routeParamName)?.ToString();
        var currentUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(currentUserId) ||
            !string.Equals(currentUserId, routeId, StringComparison.OrdinalIgnoreCase))
        {
            return Results.Forbid();
        }

        return await next(context);
    }
}
