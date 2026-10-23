using System.Security.Claims;
using AppGymAPI.Extensions;
using AppGymAPI.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace AppGymAPI.Tests;

public class AuthorizationExtensionsTests
{
    [Fact]
    public async Task ExecuteOwnerOrRoleFilterAsync_WhenUserIsAdmin_CallsNext()
    {
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        httpContext.Request.RouteValues["id"] = Guid.NewGuid().ToString();

        var context = EndpointFilterInvocationContext.Create(httpContext);
        var nextCalled = false;
        EndpointFilterDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        };

        var result = await AuthorizationExtensions.ExecuteOwnerOrRoleFilterAsync(context, next, "id", Role.Admin);

        nextCalled.Should().BeTrue();
        result.Should().BeOfType<Ok>();
    }

    [Fact]
    public async Task ExecuteOwnerOrRoleFilterAsync_WhenUserIsOwner_CallsNext()
    {
        var ownerId = Guid.NewGuid().ToString();
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.Role, "User"),
            new Claim(ClaimTypes.NameIdentifier, ownerId)
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        httpContext.Request.RouteValues["id"] = ownerId;

        var context = EndpointFilterInvocationContext.Create(httpContext);
        var nextCalled = false;
        EndpointFilterDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        };

        var result = await AuthorizationExtensions.ExecuteOwnerOrRoleFilterAsync(context, next, "id", Role.Admin);

        nextCalled.Should().BeTrue();
        result.Should().BeOfType<Ok>();
    }

    [Fact]
    public async Task ExecuteOwnerOrRoleFilterAsync_WhenUserIsNotOwnerNorAdmin_ReturnsForbid()
    {
        var currentUserId = Guid.NewGuid().ToString();
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.Role, "User"),
            new Claim(ClaimTypes.NameIdentifier, currentUserId)
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        httpContext.Request.RouteValues["id"] = Guid.NewGuid().ToString();

        var context = EndpointFilterInvocationContext.Create(httpContext);
        var nextCalled = false;
        EndpointFilterDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        };

        var result = await AuthorizationExtensions.ExecuteOwnerOrRoleFilterAsync(context, next, "id", Role.Admin);

        nextCalled.Should().BeFalse();
        result.Should().BeOfType<ForbidHttpResult>();
    }

    [Fact]
    public async Task ExecuteOwnerOrRoleFilterAsync_WhenNameIdentifierClaimMissing_ReturnsForbid()
    {
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.Role, "User")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        httpContext.Request.RouteValues["id"] = Guid.NewGuid().ToString();

        var context = EndpointFilterInvocationContext.Create(httpContext);
        var nextCalled = false;
        EndpointFilterDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        };

        var result = await AuthorizationExtensions.ExecuteOwnerOrRoleFilterAsync(context, next, "id", Role.Admin);

        nextCalled.Should().BeFalse();
        result.Should().BeOfType<ForbidHttpResult>();
    }

    [Fact]
    public async Task ExecuteOwnerOrRoleFilterAsync_WhenCustomRouteParamNameUsed_MatchesCorrectly()
    {
        var ownerId = Guid.NewGuid().ToString();
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.Role, "User"),
            new Claim(ClaimTypes.NameIdentifier, ownerId)
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        httpContext.Request.RouteValues["userId"] = ownerId;

        var context = EndpointFilterInvocationContext.Create(httpContext);
        var nextCalled = false;
        EndpointFilterDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        };

        var result = await AuthorizationExtensions.ExecuteOwnerOrRoleFilterAsync(context, next, "userId", Role.Admin);

        nextCalled.Should().BeTrue();
        result.Should().BeOfType<Ok>();
    }
}
