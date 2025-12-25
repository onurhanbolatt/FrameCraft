using FluentAssertions;
using FrameCraft.Application.Authentication.Commands.Logout;
using FrameCraft.Domain.Entities.Authentication;
using FrameCraft.Domain.Exceptions;
using FrameCraft.Domain.Repositories.Authentication;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FrameCraft.UnitTests.Authentication.Commands;

public class LogoutCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepository;
    private readonly Mock<ILogger<LogoutCommandHandler>> _mockLogger;
    private readonly LogoutCommandHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();

    public LogoutCommandHandlerTests()
    {
        _mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();
        _mockLogger = new Mock<ILogger<LogoutCommandHandler>>();

        _handler = new LogoutCommandHandler(
            _mockRefreshTokenRepository.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidRefreshToken_RevokesToken()
    {
        // Arrange
        var command = new LogoutCommand("validToken");

        var refreshToken = new RefreshToken
        {
            UserId = _userId,
            Token = "validToken",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = "127.0.0.1",
            IsRevoked = false
        };

        _mockRefreshTokenRepository
            .Setup(x => x.GetByTokenAsync(command.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockRefreshTokenRepository.Verify(
            x => x.RevokeAsync(refreshToken, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidRefreshToken_ThrowsNotFoundException()
    {
        // Arrange
        var command = new LogoutCommand("invalidToken");

        _mockRefreshTokenRepository
            .Setup(x => x.GetByTokenAsync(command.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("Refresh token bulunamadı");
    }

    [Fact]
    public async Task Handle_AlreadyRevokedToken_StillProcesses()
    {
        // Arrange
        var command = new LogoutCommand("revokedToken");

        var revokedToken = new RefreshToken
        {
            UserId = _userId,
            Token = "revokedToken",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = "127.0.0.1",
            IsRevoked = true,
            RevokedAt = DateTime.UtcNow.AddMinutes(-5),
            RevokedByIp = "127.0.0.1"
        };

        _mockRefreshTokenRepository
            .Setup(x => x.GetByTokenAsync(command.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(revokedToken);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockRefreshTokenRepository.Verify(
            x => x.RevokeAsync(revokedToken, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
