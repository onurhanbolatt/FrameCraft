using FluentValidation.TestHelper;
using FrameCraft.Application.Users.Commands.CreateUser;
using Xunit;

namespace FrameCraft.UnitTests.Validators;

public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator;

    public CreateUserCommandValidatorTests()
    {
        _validator = new CreateUserCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            TenantId = Guid.NewGuid(),
            Email = "user@test.com",
            Password = "Test123!",
            FirstName = "Test",
            LastName = "User",
            Roles = new List<string> { "User" } // ✅ KRİTİK
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyTenantId_ShouldFail()
    {
        var command = new CreateUserCommand
        {
            TenantId = Guid.Empty,
            Email = "user@test.com",
            Password = "Test123!",
            FirstName = "Test",
            LastName = "User",
            Roles = new List<string> { "User" }
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_InvalidEmail_ShouldFail()
    {
        var command = new CreateUserCommand
        {
            TenantId = Guid.NewGuid(),
            Email = "not-an-email",
            Password = "Test123!",
            FirstName = "Test",
            LastName = "User",
            Roles = new List<string> { "User" }
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_ShortPassword_ShouldFail()
    {
        var command = new CreateUserCommand
        {
            TenantId = Guid.NewGuid(),
            Email = "user@test.com",
            Password = "12345",
            FirstName = "Test",
            LastName = "User",
            Roles = new List<string> { "User" }
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_EmptyFirstName_ShouldFail()
    {
        var command = new CreateUserCommand
        {
            TenantId = Guid.NewGuid(),
            Email = "user@test.com",
            Password = "Test123!",
            FirstName = "",
            LastName = "User",
            Roles = new List<string> { "User" }
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void Validate_EmptyRoles_ShouldFail()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            TenantId = Guid.NewGuid(),
            Email = "user@test.com",
            Password = "Test123!",
            FirstName = "Test",
            LastName = "User",
            Roles = new List<string>() // ❌ boş
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Roles);
    }
}
