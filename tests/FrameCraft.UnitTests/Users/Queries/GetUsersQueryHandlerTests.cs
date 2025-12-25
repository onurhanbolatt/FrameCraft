using FluentAssertions;
using FrameCraft.Application.Users.Queries.GetUsers;
using FrameCraft.Domain.Entities.Authentication;
using FrameCraft.Domain.Repositories.Authentication;
using Moq;
using Xunit;

namespace FrameCraft.UnitTests.Users.Queries;

public class GetUsersQueryHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly GetUsersQueryHandler _handler;

    private readonly Guid _tenantAId = Guid.NewGuid();
    private readonly Guid _tenantBId = Guid.NewGuid();

    public GetUsersQueryHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _handler = new GetUsersQueryHandler(_mockUserRepository.Object);
    }

    [Fact]
    public async Task Handle_WithTenantId_ReturnsOnlyTenantUsers()
    {
        // Arrange
        var tenantAUsers = new List<User>
        {
            CreateUser(_tenantAId, "usera1@test.com"),
            CreateUser(_tenantAId, "usera2@test.com")
        };

        _mockUserRepository
            .Setup(x => x.GetByTenantIdWithRolesAsync(_tenantAId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantAUsers);

        var query = new GetUsersQuery { TenantId = _tenantAId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(u => u.Email.StartsWith("usera"));
    }

    [Fact]
    public async Task Handle_WithoutTenantId_ReturnsAllUsers()
    {
        // Arrange
        var allUsers = new List<User>
        {
            CreateUser(_tenantAId, "usera@test.com"),
            CreateUser(_tenantBId, "userb@test.com")
        };

        _mockUserRepository
            .Setup(x => x.GetAllWithRolesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allUsers);

        var query = new GetUsersQuery { TenantId = null };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithIsActiveFilter_ReturnsFilteredUsers()
    {
        // Arrange
        var users = new List<User>
        {
            CreateUser(_tenantAId, "active@test.com", isActive: true),
            CreateUser(_tenantAId, "inactive@test.com", isActive: false)
        };

        _mockUserRepository
            .Setup(x => x.GetByTenantIdWithRolesAsync(_tenantAId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var query = new GetUsersQuery
        {
            TenantId = _tenantAId,
            IsActive = true
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.Should().OnlyContain(u => u.Email == "active@test.com");
    }

    [Fact]
    public async Task Handle_WithSearchTerm_ReturnsMatchingUsers()
    {
        // Arrange
        var users = new List<User>
        {
            CreateUser(_tenantAId, "john.doe@test.com", "John", "Doe"),
            CreateUser(_tenantAId, "jane.smith@test.com", "Jane", "Smith"),
            CreateUser(_tenantAId, "admin@test.com", "Admin", "User")
        };

        _mockUserRepository
            .Setup(x => x.GetByTenantIdWithRolesAsync(_tenantAId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var query = new GetUsersQuery
        {
            TenantId = _tenantAId,
            SearchTerm = "john"
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Email.Should().Be("john.doe@test.com");
    }

    [Fact]
    public async Task Handle_SearchByLastName_ReturnsMatchingUsers()
    {
        // Arrange
        var users = new List<User>
        {
            CreateUser(_tenantAId, "john@test.com", "John", "Smith"),
            CreateUser(_tenantAId, "jane@test.com", "Jane", "Smith"),
            CreateUser(_tenantAId, "bob@test.com", "Bob", "Johnson")
        };

        _mockUserRepository
            .Setup(x => x.GetByTenantIdWithRolesAsync(_tenantAId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var query = new GetUsersQuery
        {
            TenantId = _tenantAId,
            SearchTerm = "Smith"
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(u => u.FullName.Contains("Smith"));
    }

    [Fact]
    public async Task Handle_CombinedFilters_ReturnsCorrectUsers()
    {
        // Arrange
        var users = new List<User>
        {
            CreateUser(_tenantAId, "active.john@test.com", "John", "Active", isActive: true),
            CreateUser(_tenantAId, "inactive.john@test.com", "John", "Inactive", isActive: false),
            CreateUser(_tenantAId, "active.jane@test.com", "Jane", "Active", isActive: true)
        };

        _mockUserRepository
            .Setup(x => x.GetByTenantIdWithRolesAsync(_tenantAId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var query = new GetUsersQuery
        {
            TenantId = _tenantAId,
            SearchTerm = "john",
            IsActive = true
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Email.Should().Be("active.john@test.com");
    }

    [Fact]
    public async Task Handle_WithRoles_ReturnsUserRoles()
    {
        // Arrange
        var role = new Role { Id = Guid.NewGuid(), Name = "Admin" };
        var user = CreateUser(_tenantAId, "admin@test.com");
        user.UserRoles = new List<UserRole>
        {
            new UserRole { UserId = user.Id, RoleId = role.Id, Role = role }
        };

        _mockUserRepository
            .Setup(x => x.GetByTenantIdWithRolesAsync(_tenantAId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { user });

        var query = new GetUsersQuery { TenantId = _tenantAId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Roles.Should().Contain("Admin");
    }

    [Fact]
    public async Task Handle_EmptyResult_ReturnsEmptyList()
    {
        // Arrange
        _mockUserRepository
            .Setup(x => x.GetByTenantIdWithRolesAsync(_tenantAId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());

        var query = new GetUsersQuery { TenantId = _tenantAId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_SuperAdminFlag_IsIncluded()
    {
        // Arrange
        var normalUser = CreateUser(_tenantAId, "normal@test.com");
        var superAdmin = CreateUser(_tenantAId, "super@test.com", isSuperAdmin: true);

        _mockUserRepository
            .Setup(x => x.GetByTenantIdWithRolesAsync(_tenantAId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { normalUser, superAdmin });

        var query = new GetUsersQuery { TenantId = _tenantAId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainSingle(u => u.IsSuperAdmin);
        result.Should().ContainSingle(u => !u.IsSuperAdmin);
    }

    private User CreateUser(
        Guid tenantId,
        string email,
        string firstName = "Test",
        string lastName = "User",
        bool isActive = true,
        bool isSuperAdmin = false)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email,
            PasswordHash = "hash",
            FirstName = firstName,
            LastName = lastName,
            IsActive = isActive,
            IsSuperAdmin = isSuperAdmin,
            CreatedAt = DateTime.UtcNow,
            UserRoles = new List<UserRole>()
        };
    }
}
