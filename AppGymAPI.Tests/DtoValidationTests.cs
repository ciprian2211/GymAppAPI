using System.ComponentModel.DataAnnotations;
using AppGymAPI.DTOs;
using AppGymAPI.Models;
using FluentAssertions;

namespace AppGymAPI.Tests;

public class DtoValidationTests
{
    private static IList<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model, serviceProvider: null, items: null);
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    [Theory]
    [InlineData("Al", "Li", true)]
    [InlineData("A", "Li", false)]
    [InlineData("Al", "L", false)]
    public void RegisterUserDto_NameAndSurnameValidation_EnforcesMinimumLengthOfTwo(string name, string surname, bool shouldBeValid)
    {
        var dto = new RegisterUserDto
        {
            Name = name,
            Surname = surname,
            Email = "valid@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        var results = ValidateModel(dto);

        if (shouldBeValid)
        {
            results.Should().BeEmpty();
        }
        else
        {
            results.Should().NotBeEmpty();
        }
    }

    [Theory]
    [InlineData("Al", "Li", true)]
    [InlineData("A", "Li", false)]
    public void UserEditDto_NameAndSurnameValidation_EnforcesMinimumLengthOfTwo(string name, string surname, bool shouldBeValid)
    {
        var dto = new UserEditDto
        {
            Name = name,
            Surname = surname
        };

        var results = ValidateModel(dto);

        if (shouldBeValid)
        {
            results.Should().BeEmpty();
        }
        else
        {
            results.Should().NotBeEmpty();
        }
    }

    [Theory]
    [InlineData("Row", true)]
    [InlineData("Dip", true)]
    [InlineData("Up", false)]
    public void ExerciseAddDto_NameValidation_EnforcesMinimumLengthOfThree(string name, bool shouldBeValid)
    {
        var dto = new ExerciseAddDto
        {
            Name = name,
            Description = "Valid exercise description here",
            Category = ExerciseType.Weightlifting,
            TargetedMuscle = TargetedMuscle.Chest
        };

        var results = ValidateModel(dto);

        if (shouldBeValid)
        {
            results.Should().BeEmpty();
        }
        else
        {
            results.Should().NotBeEmpty();
        }
    }
}
