using FluentAssertions;
using FrameCraft.Domain.Entities.Authentication;
using Xunit;

namespace FrameCraft.UnitTests.Domain;

public class RefreshTokenTests
{
    [Fact]
    public void Create_WithValidParameters_CreatesToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = "test-token-string";
        var expiresAt = DateTime.UtcNow.AddDays(7);
        var createdByIp = "127.0.0.1";

        // Act
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = token,
            ExpiresAt = expiresAt,
            CreatedByIp = createdByIp,
            IsRevoked = false
        };

        // Assert
        refreshToken.Should().NotBeNull();
        refreshToken.UserId.Should().Be(userId);
        refreshToken.Token.Should().Be(token);
        refreshToken.ExpiresAt.Should().Be(expiresAt);
        refreshToken.CreatedByIp.Should().Be(createdByIp);
        refreshToken.IsRevoked.Should().BeFalse();
        refreshToken.RevokedAt.Should().BeNull();
        refreshToken.RevokedByIp.Should().BeNull();
        refreshToken.IsExpired.Should().BeFalse();
        refreshToken.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_ExpiredToken_ReturnsTrue()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            UserId = Guid.NewGuid(),
            Token = "token",
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedByIp = "127.0.0.1",
            IsRevoked = false
        };

        // Assert
        refreshToken.IsExpired.Should().BeTrue();
        refreshToken.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_ValidToken_ReturnsFalse()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            UserId = Guid.NewGuid(),
            Token = "token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = "127.0.0.1",
            IsRevoked = false
        };

        // Assert
        refreshToken.IsExpired.Should().BeFalse();
        refreshToken.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_RevokedToken_ReturnsFalse()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            UserId = Guid.NewGuid(),
            Token = "token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = "127.0.0.1",
            IsRevoked = true,
            RevokedAt = DateTime.UtcNow,
            RevokedByIp = "127.0.0.1"
        };

        // Assert
        refreshToken.IsExpired.Should().BeFalse();
        refreshToken.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_ExpiredButNotRevoked_ReturnsFalse()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            UserId = Guid.NewGuid(),
            Token = "token",
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedByIp = "127.0.0.1",
            IsRevoked = false
        };

        // Assert
        refreshToken.IsExpired.Should().BeTrue();
        refreshToken.IsActive.Should().BeFalse();
    }
}
