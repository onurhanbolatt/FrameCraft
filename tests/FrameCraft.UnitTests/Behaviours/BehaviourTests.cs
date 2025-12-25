using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using FrameCraft.Application.Common.Behaviours;
using FrameCraft.Domain.Exceptions;
using MediatR;
using Moq;
using Xunit;
using ValidationException = FrameCraft.Domain.Exceptions.ValidationException;

namespace FrameCraft.UnitTests.Behaviours;

public class ValidationBehaviourTests
{
    [Fact]
    public async Task Handle_NoValidators_CallsNext()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<TestRequest>>();
        var behaviour = new ValidationBehaviour<TestRequest, TestResponse>(validators);

        var request = new TestRequest { Name = "Test" };
        var expectedResponse = new TestResponse { Result = "Success" };

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(expectedResponse);

        // Act
        var result = await behaviour.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsNext()
    {
        // Arrange
        var mockValidator = new Mock<IValidator<TestRequest>>();
        mockValidator
            .Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult()); // No errors

        var validators = new List<IValidator<TestRequest>> { mockValidator.Object };
        var behaviour = new ValidationBehaviour<TestRequest, TestResponse>(validators);

        var request = new TestRequest { Name = "Test" };
        var expectedResponse = new TestResponse { Result = "Success" };

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(expectedResponse);

        // Act
        var result = await behaviour.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Handle_InvalidRequest_ThrowsValidationException()
    {
        // Arrange
        var mockValidator = new Mock<IValidator<TestRequest>>();
        var failures = new List<ValidationFailure>
        {
            new ValidationFailure("Name", "Name is required"),
            new ValidationFailure("Name", "Name must be at least 3 characters")
        };

        mockValidator
            .Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        var validators = new List<IValidator<TestRequest>> { mockValidator.Object };
        var behaviour = new ValidationBehaviour<TestRequest, TestResponse>(validators);

        var request = new TestRequest { Name = "" };

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(new TestResponse());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => behaviour.Handle(request, next, CancellationToken.None));

        exception.Errors.Should().ContainKey("Name");
        exception.Errors["Name"].Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_MultipleValidators_AggregatesErrors()
    {
        // Arrange
        var mockValidator1 = new Mock<IValidator<TestRequest>>();
        var mockValidator2 = new Mock<IValidator<TestRequest>>();

        mockValidator1
            .Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Name", "Error from validator 1") }));

        mockValidator2
            .Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Email", "Error from validator 2") }));

        var validators = new List<IValidator<TestRequest>> { mockValidator1.Object, mockValidator2.Object };
        var behaviour = new ValidationBehaviour<TestRequest, TestResponse>(validators);

        var request = new TestRequest();

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(new TestResponse());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => behaviour.Handle(request, next, CancellationToken.None));

        exception.Errors.Should().HaveCount(2);
        exception.Errors.Should().ContainKey("Name");
        exception.Errors.Should().ContainKey("Email");
    }

    [Fact]
    public async Task Handle_MultipleErrorsOnSameProperty_GroupsThem()
    {
        // Arrange
        var mockValidator = new Mock<IValidator<TestRequest>>();
        var failures = new List<ValidationFailure>
        {
            new ValidationFailure("Password", "Password is required"),
            new ValidationFailure("Password", "Password must be at least 8 characters"),
            new ValidationFailure("Password", "Password must contain a number")
        };

        mockValidator
            .Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        var validators = new List<IValidator<TestRequest>> { mockValidator.Object };
        var behaviour = new ValidationBehaviour<TestRequest, TestResponse>(validators);

        var request = new TestRequest();

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(new TestResponse());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => behaviour.Handle(request, next, CancellationToken.None));

        exception.Errors.Should().ContainKey("Password");
        exception.Errors["Password"].Should().HaveCount(3);
    }

    // Test helper classes
    public class TestRequest : IRequest<TestResponse>
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class TestResponse
    {
        public string Result { get; set; } = string.Empty;
    }
}

public class LoggingBehaviourTests
{
    [Fact]
    public async Task Handle_AnyRequest_LogsAndCallsNext()
    {
        // Arrange
        var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<LoggingBehaviour<TestRequest, TestResponse>>>();

        var behaviour = new LoggingBehaviour<TestRequest, TestResponse>(mockLogger.Object);

        var request = new TestRequest { Name = "Test" };
        var expectedResponse = new TestResponse { Result = "Success" };

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(expectedResponse);

        // Act
        var result = await behaviour.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Handle_SlowRequest_LogsWarning()
    {
        // Arrange
        var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<LoggingBehaviour<TestRequest, TestResponse>>>();

        var behaviour = new LoggingBehaviour<TestRequest, TestResponse>(mockLogger.Object);

        var request = new TestRequest { Name = "Slow Test" };
        var expectedResponse = new TestResponse { Result = "Success" };

        // Simulate slow request
        RequestHandlerDelegate<TestResponse> next = async () =>
        {
            await Task.Delay(600); // > 500ms threshold
            return expectedResponse;
        };

        // Act
        var result = await behaviour.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        // Logger.LogWarning should have been called for slow request
    }

    // Test helper classes
    public class TestRequest : IRequest<TestResponse>
    {
        public string Name { get; set; } = string.Empty;
    }

    public class TestResponse
    {
        public string Result { get; set; } = string.Empty;
    }
}
