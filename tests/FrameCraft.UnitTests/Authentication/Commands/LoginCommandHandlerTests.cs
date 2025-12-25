using FluentAssertions;
using FrameCraft.Application.Authentication.Commands.Login;
using FrameCraft.Application.Common.Interfaces;
using FrameCraft.Application.Common.Models;
using FrameCraft.Domain.Entities.Authentication;
using FrameCraft.Domain.Exceptions;
using FrameCraft.Domain.Repositories.Authentication;
using FrameCraft.Domain.Repositories.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace FrameCraft.UnitTests.Authentication.Commands;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepository;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<ITenantRepository> _mockTenantRepository;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<ILogger<LoginCommandHandler>> _mockLogger;
    private readonly JwtSettings _jwtSettings;
    private readonly LoginCommandHandler _handler;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public LoginCommandHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();
        _mockTenantRepository = new Mock<ITenantRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockTokenService = new Mock<ITokenService>();
        _mockLogger = new Mock<ILogger<LoginCommandHandler>>();

        _jwtSettings = new JwtSettings
        {
            Secret = "TestSecretKeyThatIsAtLeast32Characters!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        };

        var options = Options.Create(_jwtSettings);
        _handler = new LoginCommandHandler(
            _mockUserRepository.Object,
            _mockTenantRepository.Object,
            _mockRefreshTokenRepository.Object,
            _mockPasswordHasher.Object,
            _mockTokenService.Object,
            options,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsLoginResponse()
    {
        var command = new LoginCommand
        {
            Email = "test@test.com",
            Password = "Test123!",
            IpAddress = "127.0.0.1"
        };

        var user = new User
        {
            Id = _userId,
            TenantId = _tenantId,
            Email = "test@test.com",
            PasswordHash = "hashedPassword",
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
            IsSuperAdmin = false
        };

        var roles = new List<string> { "User" };

        var refreshToken = new RefreshToken
        {
            UserId = _userId,
            Token = "refreshToken123",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = "127.0.0.1",
            IsRevoked = false
        };

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordHasher
            .Setup(x => x.VerifyPassword(command.Password, user.PasswordHash))
            .Returns(true);

        _mockUserRepository
            .Setup(x => x.GetUserRolesAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        _mockTokenService
            .Setup(x => x.GenerateAccessToken(user, roles))
            .Returns(("accessToken123", DateTime.UtcNow.AddHours(1)));

        _mockTokenService
            .Setup(x => x.CreateRefreshTokenAsync(_userId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.UserId.Should().Be(_userId);
        result.Email.Should().Be("test@test.com");
        result.AccessToken.Should().Be("accessToken123");
        result.RefreshToken.Should().Be("refreshToken123");
        result.TenantId.Should().Be(_tenantId);
        result.Roles.Should().Contain("User");
    }

    [Fact]
    public async Task Handle_InvalidEmail_ThrowsBadRequestException()
    {
        var command = new LoginCommand
        {
            Email = "notexist@test.com",
            Password = "Test123!"
        };

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("Email veya şifre hatalı");
    }

    [Fact]
    public async Task Handle_InvalidPassword_ThrowsBadRequestException()
    {
        var command = new LoginCommand
        {
            Email = "test@test.com",
            Password = "WrongPassword!"
        };

        var user = new User
        {
            Id = _userId,
            Email = "test@test.com",
            PasswordHash = "hashedPassword",
            IsActive = true
        };

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordHasher
            .Setup(x => x.VerifyPassword(command.Password, user.PasswordHash))
            .Returns(false);

        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("Email veya şifre hatalı");
    }

    [Fact]
    public async Task Handle_InactiveUser_ThrowsBadRequestException()
    {
        var command = new LoginCommand
        {
            Email = "test@test.com",
            Password = "Test123!"
        };

        var user = new User
        {
            Id = _userId,
            Email = "test@test.com",
            PasswordHash = "hashedPassword",
            IsActive = false
        };

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordHasher
            .Setup(x => x.VerifyPassword(command.Password, user.PasswordHash))
            .Returns(true);

        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("pasif");
    }

    [Fact]
    public async Task Handle_SuperAdminLogin_ReturnsSuperAdminFlag()
    {
        var command = new LoginCommand
        {
            Email = "admin@system.com",
            Password = "Admin123!"
        };

        var superAdmin = new User
        {
            Id = _userId,
            TenantId = Guid.Empty,
            Email = "admin@system.com",
            PasswordHash = "hashedPassword",
            IsActive = true,
            IsSuperAdmin = true
        };

        var roles = new List<string> { "SuperAdmin" };

        var refreshToken = new RefreshToken
        {
            UserId = _userId,
            Token = "refreshToken",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = "127.0.0.1",
            IsRevoked = false
        };

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(superAdmin);

        _mockPasswordHasher
            .Setup(x => x.VerifyPassword(command.Password, superAdmin.PasswordHash))
            .Returns(true);

        _mockUserRepository
            .Setup(x => x.GetUserRolesAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        _mockTokenService
            .Setup(x => x.GenerateAccessToken(superAdmin, roles))
            .Returns(("accessToken", DateTime.UtcNow.AddHours(1)));

        _mockTokenService
            .Setup(x => x.CreateRefreshTokenAsync(_userId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuperAdmin.Should().BeTrue();
        result.Roles.Should().Contain("SuperAdmin");
    }

    [Fact]
    public async Task Handle_SuccessfulLogin_UpdatesLastLoginTime()
    {
        var command = new LoginCommand
        {
            Email = "test@test.com",
            Password = "Test123!"
        };

        var user = new User
        {
            Id = _userId,
            Email = "test@test.com",
            PasswordHash = "hashedPassword",
            IsActive = true,
            LastLoginAt = null
        };

        var refreshToken = new RefreshToken
        {
            UserId = _userId,
            Token = "refreshToken",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = "127.0.0.1",
            IsRevoked = false
        };

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordHasher
            .Setup(x => x.VerifyPassword(command.Password, user.PasswordHash))
            .Returns(true);

        _mockUserRepository
            .Setup(x => x.GetUserRolesAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _mockTokenService
            .Setup(x => x.GenerateAccessToken(user, It.IsAny<List<string>>()))
            .Returns(("accessToken", DateTime.UtcNow.AddHours(1)));

        _mockTokenService
            .Setup(x => x.CreateRefreshTokenAsync(_userId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);

        await _handler.Handle(command, CancellationToken.None);

        _mockUserRepository.Verify(x => x.UpdateAsync(
            It.Is<User>(u => u.LastLoginAt != null),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
