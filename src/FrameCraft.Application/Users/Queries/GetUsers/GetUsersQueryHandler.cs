using FrameCraft.Application.Users.DTOs;
using FrameCraft.Domain.Repositories.Authentication;
using MediatR;

namespace FrameCraft.Application.Users.Queries.GetUsers;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, List<UserSummaryDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<List<UserSummaryDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        // Tenant filtresi varsa ona göre çek, yoksa hepsini çek
        var users = request.TenantId.HasValue
            ? await _userRepository.GetByTenantIdWithRolesAsync(request.TenantId.Value, cancellationToken)
            : await _userRepository.GetAllWithRolesAsync(cancellationToken);

        var query = users.AsQueryable();

        // Aktif/Pasif filtresi
        if (request.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == request.IsActive.Value);
        }

        // Arama filtresi
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var searchLower = request.SearchTerm.ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(searchLower) ||
                u.FirstName.ToLower().Contains(searchLower) ||
                u.LastName.ToLower().Contains(searchLower));
        }

        var result = new List<UserSummaryDto>();
        
        foreach (var u in query)
        {
            var roles = new List<string>();
            if (u.UserRoles != null)
            {
                foreach (var ur in u.UserRoles)
                {
                    if (ur.Role != null && !string.IsNullOrEmpty(ur.Role.Name))
                    {
                        roles.Add(ur.Role.Name);
                    }
                }
            }

            result.Add(new UserSummaryDto
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                IsActive = u.IsActive,
                IsSuperAdmin = u.IsSuperAdmin,
                Roles = roles,
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt
            });
        }

        return result;
    }
}
