using FrameCraft.Application.Common.Interfaces;
using FrameCraft.Application.TenantManagement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FrameCraft.Application.TenantManagement.Queries;

// ============================================
// Get Tenant Users Query
// ============================================

public record GetTenantUsersQuery(Guid TenantId) : IRequest<List<TenantUserDto>>;

public class GetTenantUsersQueryHandler : IRequestHandler<GetTenantUsersQuery, List<TenantUserDto>>
{
    private readonly IApplicationDbContext _context;

    public GetTenantUsersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<TenantUserDto>> Handle(GetTenantUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await _context.Users
            .AsNoTracking()
            .Where(u => u.TenantId == request.TenantId && !u.IsSuperAdmin)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new TenantUserDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                FullName = $"{u.FirstName} {u.LastName}",
                IsActive = u.IsActive,
                Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList(),
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            })
            .ToListAsync(cancellationToken);

        return users;
    }
}

// ============================================
// Get Tenant User By Id Query
// ============================================

public record GetTenantUserByIdQuery(Guid UserId, Guid TenantId) : IRequest<TenantUserDto?>;

public class GetTenantUserByIdQueryHandler : IRequestHandler<GetTenantUserByIdQuery, TenantUserDto?>
{
    private readonly IApplicationDbContext _context;

    public GetTenantUserByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TenantUserDto?> Handle(GetTenantUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == request.UserId && u.TenantId == request.TenantId && !u.IsSuperAdmin)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Select(u => new TenantUserDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                FullName = $"{u.FirstName} {u.LastName}",
                IsActive = u.IsActive,
                Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList(),
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return user;
    }
}

// ============================================
// Get Tenant Info Query
// ============================================

public record GetTenantInfoQuery(Guid TenantId) : IRequest<TenantInfoDto?>;

public class GetTenantInfoQueryHandler : IRequestHandler<GetTenantInfoQuery, TenantInfoDto?>
{
    private readonly IApplicationDbContext _context;

    public GetTenantInfoQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TenantInfoDto?> Handle(GetTenantInfoQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants
            .AsNoTracking()
            .Where(t => t.Id == request.TenantId)
            .Select(t => new TenantInfoDto
            {
                Id = t.Id,
                Name = t.Name,
                Subdomain = t.Subdomain,
                Email = t.Email,
                Phone = t.Phone,
                Status = t.Status.ToString(),
                SubscriptionPlan = t.SubscriptionPlan,
                MaxUsers = t.MaxUsers,
                CreatedAt = t.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (tenant != null)
        {
            tenant.CurrentUserCount = await _context.Users
                .CountAsync(u => u.TenantId == request.TenantId && !u.IsDeleted, cancellationToken);
        }

        return tenant;
    }
}