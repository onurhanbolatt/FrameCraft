using FrameCraft.Application.Customers.Commands.CreateCustomer;
using FluentAssertions;
using Xunit;

namespace FrameCraft.UnitTests.Customers.Validators;

public class CreateCustomerCommandValidatorTests
{
    private readonly CreateCustomerCommandValidator _validator;

    public CreateCustomerCommandValidatorTests()
    {
        _validator = new CreateCustomerCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_ReturnsNoErrors()
    {
        // Arrange
        var command = new CreateCustomerCommand
        {
            Name = "Ahmet Yılmaz",
            Email = "ahmet@test.com",
            Phone = "05551234567",
            Address = "İstanbul",
            IsActive = true
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_EmptyName_ReturnsError()
    {
        // Arrange
        var command = new CreateCustomerCommand
        {
            Name = "",
            Email = "test@test.com"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_NameTooShort_ReturnsError()
    {
        // Arrange
        var command = new CreateCustomerCommand
        {
            Name = "A", // 1 karakter (min 2)
            Email = "test@test.com"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_InvalidEmail_ReturnsError()
    {
        // Arrange
        var command = new CreateCustomerCommand
        {
            Name = "Ahmet Yılmaz",
            Email = "invalid-email"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_InvalidPhoneFormat_ReturnsError()
    {
        // Arrange
        var command = new CreateCustomerCommand
        {
            Name = "Ahmet Yılmaz",
            Phone = "ABC123" // Geçersiz format
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Phone");
    }

    [Theory]
    [InlineData("05551234567")]
    [InlineData("0555 123 45 67")]
    [InlineData("+90 555 123 45 67")]
    [InlineData("(555) 123-45-67")]
    public void Validate_ValidPhoneFormats_ReturnsNoErrors(string phone)
    {
        // Arrange
        var command = new CreateCustomerCommand
        {
            Name = "Ahmet Yılmaz",
            Phone = phone
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.Errors.Should().NotContain(e => e.PropertyName == "Phone");
    }

    [Fact]
    public void Validate_NameTooLong_ReturnsError()
    {
        // Arrange
        var command = new CreateCustomerCommand
        {
            Name = new string('A', 201), // 201 karakter (max 200)
            Email = "test@test.com"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }
}
