using FluentAssertions;
using FrameCraft.Application.Authentication.Commands.RefreshToken;
using FrameCraft.Application.Common.Interfaces;
using FrameCraft.Domain.Entities.Authentication;
using FrameCraft.Domain.Exceptions;
using FrameCraft.Domain.Repositories.Authentication;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using DomainRefreshToken = FrameCraft.Domain.Entities.Authentication.RefreshToken;

namespace FrameCraft.UnitTests.Authentication.Commands;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<ILogger<RefreshTokenCommandHandler>> _mockLogger;
    private readonly RefreshTokenCommandHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();

    public RefreshTokenCommandHandlerTests()
    {
        _mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockTokenService = new Mock<ITokenService>();
        _mockLogger = new Mock<ILogger<RefreshTokenCommandHandler>>();

        _handler = new RefreshTokenCommandHandler(
            _mockRefreshTokenRepository.Object,
            _mockUserRepository.Object,
            _mockTokenService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidRefreshToken_ReturnsNewTokens()
    {
        // Arrange
        var command = new RefreshTokenCommand
        {
            RefreshToken = "validRefreshToken",
            IpAddress = "127.0.0.1"
        };

        var existingToken = new DomainRefreshToken
        {
            UserId = _userId,
            Token = "validRefreshToken",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = "127.0.0.1",
            IsRevoked = false
        };

        var user = new User
        {
            Id = _userId,
            TenantId = _tenantId,
            Email = "test@test.com",
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
            IsSuperAdmin = false
        };

        var roles = new List<string> { "User" };

        var newRefreshToken = new DomainRefreshToken
        {
            UserId = _userId,
            Token = "newRefreshToken",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = "127.0.0.1",
            IsRevoked = false
        };

        _mockRefreshTokenRepository
            .Setup(x => x.GetByTokenAsync(command.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingToken);

        _mockUserRepository
            .Setup(x => x.GetByIdWithRolesAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUserRepository
            .Setup(x => x.GetUserRolesAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        _mockTokenService
            .Setup(x => x.GenerateAccessToken(user, roles))
            .Returns(("newAccessToken", DateTime.UtcNow.AddHours(1)));

        _mockTokenService
            .Setup(x => x.CreateRefreshTokenAsync(_userId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newRefreshToken);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("newAccessToken");
        result.RefreshToken.Should().Be("newRefreshToken");
        result.UserId.Should().Be(_userId);
    }

    [Fact]
    public async Task Handle_InvalidRefreshToken_ThrowsUnauthorizedException()
    {
        // Arrange
        var command = new RefreshTokenCommand
        {
            RefreshToken = "invalidToken"
        };

        _mockRefreshTokenRepository
            .Setup(x => x.GetByTokenAsync(command.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainRefreshToken?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("Geçersiz refresh token");
    }

    [Fact]
    public async Task Handle_ExpiredRefreshToken_ThrowsUnauthorizedException()
    {
        // Arrange
        var command = new RefreshTokenCommand
        {
            RefreshToken = "expiredToken"
        };

        var expiredToken = new DomainRefreshToken
        {
            UserId = _userId,
            Token = "expiredToken",
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedByIp = "127.0.0.1",
            IsRevoked = false
        };

        _mockRefreshTokenRepository
            .Setup(x => x.GetByTokenAsync(command.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredToken);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("süresi dolmuş");
    }

    [Fact]
    public async Task Handle_RevokedRefreshToken_ThrowsUnauthorizedException()
    {
        // Arrange
        var command = new RefreshTokenCommand
        {
            RefreshToken = "revokedToken"
        };

        var revokedToken = new DomainRefreshToken
        {
            UserId = _userId,
            Token = "revokedToken",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = "127.0.0.1",
            IsRevoked = true,
            RevokedAt = DateTime.UtcNow.AddMinutes(-10),
            RevokedByIp = "127.0.0.1"
        };

        _mockRefreshTokenRepository
            .Setup(x => x.GetByTokenAsync(command.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(revokedToken);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("iptal edilmiş");
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var command = new RefreshTokenCommand
        {
            RefreshToken = "validToken"
        };

        var token = new DomainRefreshToken
        {
            UserId = _userId,
            Token = "validToken",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = "127.0.0.1",
            IsRevoked = false
        };

        _mockRefreshTokenRepository
            .Setup(x => x.GetByTokenAsync(command.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        _mockUserRepository
            .Setup(x => x.GetByIdWithRolesAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("Kullanıcı bulunamadı");
    }

    [Fact]
    public async Task Handle_InactiveUser_ThrowsBadRequestException()
    {
        // Arrange
        var command = new RefreshTokenCommand
        {
            RefreshToken = "validToken"
        };

        var token = new DomainRefreshToken
        {
            UserId = _userId,
            Token = "validToken",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = "127.0.0.1",
            IsRevoked = false
        };

        var inactiveUser = new User
        {
            Id = _userId,
            Email = "inactive@test.com",
            IsActive = false
        };

        _mockRefreshTokenRepository
            .Setup(x => x.GetByTokenAsync(command.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        _mockUserRepository
            .Setup(x => x.GetByIdWithRolesAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveUser);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("pasif");
    }

    [Fact]
    public async Task Handle_ValidToken_RevokesOldToken()
    {
        // Arrange
        var command = new RefreshTokenCommand
        {
            RefreshToken = "oldToken",
            IpAddress = "127.0.0.1"
        };

        var oldToken = new DomainRefreshToken
        {
            UserId = _userId,
            Token = "oldToken",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = "127.0.0.1",
            IsRevoked = false
        };

        var user = new User
        {
            Id = _userId,
            Email = "test@test.com",
            FirstName = "Test",
            LastName = "User",
            IsActive = true
        };

        var newToken = new DomainRefreshToken
        {
            UserId = _userId,
            Token = "newToken",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = "127.0.0.1",
            IsRevoked = false
        };

        _mockRefreshTokenRepository
            .Setup(x => x.GetByTokenAsync(command.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(oldToken);

        _mockUserRepository
            .Setup(x => x.GetByIdWithRolesAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUserRepository
            .Setup(x => x.GetUserRolesAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _mockTokenService
            .Setup(x => x.GenerateAccessToken(user, It.IsAny<List<string>>()))
            .Returns(("newAccessToken", DateTime.UtcNow.AddHours(1)));

        _mockTokenService
            .Setup(x => x.CreateRefreshTokenAsync(_userId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newToken);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockRefreshTokenRepository.Verify(
            x => x.RevokeAsync(oldToken, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
