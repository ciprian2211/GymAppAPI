using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AppGymAPI.Tests;

public class ValidationRegistrationTests
{
    [Fact]
    public void AddValidation_RegistersValidationServicesInServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddValidation();

        services.Should().NotBeEmpty();
    }
}
