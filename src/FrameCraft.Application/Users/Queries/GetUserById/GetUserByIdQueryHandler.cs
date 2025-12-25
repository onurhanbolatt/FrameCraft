using FrameCraft.Application.Users.DTOs;
using FrameCraft.Domain.Repositories;
using FrameCraft.Domain.Repositories.Authentication;
using MediatR;

namespace FrameCraft.Application.Users.Queries.GetUserById;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto?>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdWithRolesAsync(request.UserId, cancellationToken);

        if (user == null || user.IsDeleted)
        {
            return null;
        }

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            IsSuperAdmin = user.IsSuperAdmin,
            TenantId = user.TenantId,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            Roles = user.UserRoles?.Select(ur => ur.Role?.Name ?? "").ToList() ?? new List<string>()
        };
    }
}