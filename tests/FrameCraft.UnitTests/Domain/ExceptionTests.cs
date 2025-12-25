using FluentAssertions;
using FrameCraft.Domain.Exceptions;
using Xunit;

namespace FrameCraft.UnitTests.Domain;

public class ExceptionTests
{
    [Fact]
    public void NotFoundException_WithMessage_SetsMessageCorrectly()
    {
        // Arrange & Act
        var exception = new NotFoundException("Entity not found");

        // Assert
        exception.Message.Should().Be("Entity not found");
    }

    [Fact]
    public void NotFoundException_WithEntityNameAndKey_FormatsMessage()
    {
        // Arrange & Act
        var exception = new NotFoundException("Customer", Guid.NewGuid());

        // Assert
        exception.Message.Should().Contain("Customer");
    }

    [Fact]
    public void BadRequestException_WithMessage_SetsMessageCorrectly()
    {
        // Arrange & Act
        var exception = new BadRequestException("Invalid request");

        // Assert
        exception.Message.Should().Be("Invalid request");
    }

    [Fact]
    public void UnauthorizedException_WithMessage_SetsMessageCorrectly()
    {
        // Arrange & Act
        var exception = new UnauthorizedException("Access denied");

        // Assert
        exception.Message.Should().Be("Access denied");
    }

    [Fact]
    public void ValidationException_WithErrors_ContainsAllErrors()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            { "Name", new[] { "Name is required", "Name is too short" } },
            { "Email", new[] { "Email is invalid" } }
        };

        // Act
        var exception = new ValidationException(errors);

        // Assert
        exception.Errors.Should().HaveCount(2);
        exception.Errors["Name"].Should().HaveCount(2);
        exception.Errors["Email"].Should().HaveCount(1);
    }

    [Fact]
    public void ValidationException_EmptyErrors_HasEmptyDictionary()
    {
        // Arrange & Act
        var exception = new ValidationException(new Dictionary<string, string[]>());

        // Assert
        exception.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ForbiddenAccessException_DefaultMessage_IsCorrect()
    {
        // Arrange & Act
        var exception = new ForbiddenAccessException();

        // Assert
        exception.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ForbiddenAccessException_WithMessage_SetsMessageCorrectly()
    {
        // Arrange & Act
        var exception = new ForbiddenAccessException("You don't have permission");

        // Assert
        exception.Message.Should().Be("You don't have permission");
    }
}
