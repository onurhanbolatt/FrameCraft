using FluentAssertions;
using FrameCraft.Application.Tenants.Queries.GetTenantById;
using FrameCraft.Domain.Entities.Authentication;
using FrameCraft.Domain.Entities.Core;
using FrameCraft.Domain.Enums;
using FrameCraft.Domain.Exceptions;
using FrameCraft.Domain.Repositories.Core;
using Moq;
using Xunit;

namespace FrameCraft.UnitTests.Tenants.Queries;

public class GetTenantByIdQueryHandlerTests
{
    private readonly Mock<ITenantRepository> _mockTenantRepository;
    private readonly GetTenantByIdQueryHandler _handler;

    public GetTenantByIdQueryHandlerTests()
    {
        _mockTenantRepository = new Mock<ITenantRepository>();
        _handler = new GetTenantByIdQueryHandler(_mockTenantRepository.Object);
    }

    [Fact]
    public async Task Handle_ExistingTenant_ReturnsTenant()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Test Company",
            Subdomain = "test-company",
            Status = TenantStatus.Active,
            Users = new List<User>()
        };

        _mockTenantRepository
            .Setup(x => x.GetByIdWithUsersAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var query = new GetTenantByIdQuery { Id = tenantId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(tenantId);
        result.Name.Should().Be("Test Company");
        result.Subdomain.Should().Be("test-company");
        result.Status.Should().Be(TenantStatus.Active);
        result.UserCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_NonExistentTenant_ThrowsNotFoundException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        _mockTenantRepository
            .Setup(x => x.GetByIdWithUsersAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var query = new GetTenantByIdQuery { Id = tenantId };

        // Act
        var act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*Tenant bulunamadı: {tenantId}*");
    }

    [Fact]
    public async Task Handle_TenantWithUsers_ReturnsUserCount()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Test Company",
            Subdomain = "test-company",
            Users = new List<User>
            {
                new() { Id = Guid.NewGuid(), IsDeleted = false },
                new() { Id = Guid.NewGuid(), IsDeleted = false },
                new() { Id = Guid.NewGuid(), IsDeleted = true } 
            }
        };

        _mockTenantRepository
            .Setup(x => x.GetByIdWithUsersAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var query = new GetTenantByIdQuery { Id = tenantId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserCount.Should().Be(3); // ✅ handler mantığı bu
    }
}
