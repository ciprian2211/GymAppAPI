using FluentAssertions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AppGymAPI.Tests;

public class CorsRegistrationTests
{
    [Fact]
    public void AddCors_RegistersCorsServices_AllowsResolvingCorsServiceAndPolicyProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCors();
        var serviceProvider = services.BuildServiceProvider();

        serviceProvider.GetService<ICorsService>().Should().NotBeNull();
        serviceProvider.GetService<ICorsPolicyProvider>().Should().NotBeNull();
    }
}
