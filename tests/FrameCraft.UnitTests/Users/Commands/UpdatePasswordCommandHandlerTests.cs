using FluentAssertions;
using FrameCraft.Application.Common.Interfaces;
using FrameCraft.Application.Users.Commands.UpdatePassword;
using FrameCraft.Domain.Entities.Authentication;
using FrameCraft.Domain.Exceptions;
using FrameCraft.Domain.Repositories.Authentication;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FrameCraft.UnitTests.Users.Commands;

public class UpdatePasswordCommandHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<ILogger<UpdatePasswordCommandHandler>> _mockLogger;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly UpdatePasswordCommandHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();

    public UpdatePasswordCommandHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockLogger = new Mock<ILogger<UpdatePasswordCommandHandler>>();

        // Default setup: Kullanıcı kendi hesabıyla işlem yapıyor
        _mockCurrentUserService.Setup(x => x.UserId).Returns(_userId);
        _mockTenantContext.Setup(x => x.CurrentTenantId).Returns(_tenantId);
        _mockTenantContext.Setup(x => x.IsSuperAdmin).Returns(false);

        _handler = new UpdatePasswordCommandHandler(
            _mockUserRepository.Object,
            _mockPasswordHasher.Object,
            _mockCurrentUserService.Object,
            _mockTenantContext.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidCurrentPassword_UpdatesPassword()
    {
        // Arrange
        var command = new UpdatePasswordCommand
        {
            UserId = _userId,
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            IsAdminReset = false
        };

        var user = new User
        {
            Id = _userId,
            TenantId = _tenantId,
            Email = "test@test.com",
            PasswordHash = "oldHash",
            IsDeleted = false
        };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordHasher
            .Setup(x => x.VerifyPassword(command.CurrentPassword, user.PasswordHash))
            .Returns(true);

        _mockPasswordHasher
            .Setup(x => x.HashPassword(command.NewPassword))
            .Returns("newHash");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _mockUserRepository.Verify(
            x => x.UpdateAsync(It.Is<User>(u => u.PasswordHash == "newHash"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WrongCurrentPassword_ThrowsBadRequestException()
    {
        // Arrange
        var command = new UpdatePasswordCommand
        {
            UserId = _userId,
            CurrentPassword = "WrongPassword!",
            NewPassword = "NewPassword123!",
            IsAdminReset = false
        };

        var user = new User
        {
            Id = _userId,
            TenantId = _tenantId,
            PasswordHash = "correctHash",
            IsDeleted = false
        };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordHasher
            .Setup(x => x.VerifyPassword(command.CurrentPassword, user.PasswordHash))
            .Returns(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("Mevcut şifre hatalı");
    }

    [Fact]
    public async Task Handle_AdminReset_SameTenant_SkipsCurrentPasswordCheck()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        
        var command = new UpdatePasswordCommand
        {
            UserId = targetUserId,
            CurrentPassword = null, // No current password needed
            NewPassword = "NewPassword123!",
            IsAdminReset = true
        };

        var user = new User
        {
            Id = targetUserId,
            TenantId = _tenantId, // Aynı tenant
            PasswordHash = "oldHash",
            IsDeleted = false
        };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordHasher
            .Setup(x => x.HashPassword(command.NewPassword))
            .Returns("newHash");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        // Verify current password was NOT checked
        _mockPasswordHasher.Verify(
            x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_AdminReset_DifferentTenant_ThrowsForbiddenAccessException()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        var differentTenantId = Guid.NewGuid();
        
        var command = new UpdatePasswordCommand
        {
            UserId = targetUserId,
            NewPassword = "NewPassword123!",
            IsAdminReset = true
        };

        var user = new User
        {
            Id = targetUserId,
            TenantId = differentTenantId, // Farklı tenant!
            PasswordHash = "oldHash",
            IsDeleted = false
        };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("yetkiniz yok");
    }

    [Fact]
    public async Task Handle_SuperAdminReset_AnyTenant_Succeeds()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        var differentTenantId = Guid.NewGuid();
        
        // SuperAdmin olarak ayarla
        _mockTenantContext.Setup(x => x.IsSuperAdmin).Returns(true);

        var command = new UpdatePasswordCommand
        {
            UserId = targetUserId,
            NewPassword = "NewPassword123!",
            IsAdminReset = true
        };

        var user = new User
        {
            Id = targetUserId,
            TenantId = differentTenantId, // Farklı tenant ama SuperAdmin için sorun değil
            PasswordHash = "oldHash",
            IsDeleted = false
        };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordHasher
            .Setup(x => x.HashPassword(command.NewPassword))
            .Returns("newHash");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DifferentUser_NonAdmin_ThrowsForbiddenAccessException()
    {
        // Arrange
        var differentUserId = Guid.NewGuid();
        
        var command = new UpdatePasswordCommand
        {
            UserId = differentUserId, // Farklı kullanıcının ID'si
            CurrentPassword = "SomePassword!",
            NewPassword = "NewPassword123!",
            IsAdminReset = false
        };

        var user = new User
        {
            Id = differentUserId,
            TenantId = _tenantId,
            PasswordHash = "hash",
            IsDeleted = false
        };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(differentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("kendi şifrenizi");
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var command = new UpdatePasswordCommand
        {
            UserId = _userId,
            CurrentPassword = "OldPassword!",
            NewPassword = "NewPassword!"
        };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("Kullanıcı bulunamadı");
    }

    [Fact]
    public async Task Handle_DeletedUser_ThrowsNotFoundException()
    {
        // Arrange
        var command = new UpdatePasswordCommand
        {
            UserId = _userId,
            CurrentPassword = "OldPassword!",
            NewPassword = "NewPassword!"
        };

        var deletedUser = new User
        {
            Id = _userId,
            IsDeleted = true
        };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deletedUser);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NonAdminReset_WithoutCurrentPassword_ThrowsBadRequestException()
    {
        // Arrange
        var command = new UpdatePasswordCommand
        {
            UserId = _userId,
            CurrentPassword = null, // Missing
            NewPassword = "NewPassword123!",
            IsAdminReset = false
        };

        var user = new User
        {
            Id = _userId,
            TenantId = _tenantId,
            PasswordHash = "hash",
            IsDeleted = false
        };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("Mevcut şifre gereklidir");
    }

    [Fact]
    public async Task Handle_SuccessfulUpdate_SetsUpdatedAt()
    {
        // Arrange
        var command = new UpdatePasswordCommand
        {
            UserId = _userId,
            CurrentPassword = "OldPassword!",
            NewPassword = "NewPassword!",
            IsAdminReset = false
        };

        var user = new User
        {
            Id = _userId,
            TenantId = _tenantId,
            PasswordHash = "oldHash",
            IsDeleted = false,
            UpdatedAt = null
        };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordHasher
            .Setup(x => x.VerifyPassword(command.CurrentPassword, user.PasswordHash))
            .Returns(true);

        _mockPasswordHasher
            .Setup(x => x.HashPassword(command.NewPassword))
            .Returns("newHash");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockUserRepository.Verify(
            x => x.UpdateAsync(It.Is<User>(u => u.UpdatedAt != null), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
