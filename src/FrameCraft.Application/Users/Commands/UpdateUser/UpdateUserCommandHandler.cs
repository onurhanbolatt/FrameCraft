using FrameCraft.Domain.Exceptions;
using FrameCraft.Domain.Repositories;
using FrameCraft.Domain.Repositories.Authentication;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FrameCraft.Application.Users.Commands.UpdateUser;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ILogger<UpdateUserCommandHandler> _logger;

    public UpdateUserCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ILogger<UpdateUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdWithRolesAsync(request.UserId, cancellationToken);

        if (user == null || user.IsDeleted)
        {
            throw new NotFoundException("Kullanıcı bulunamadı");
        }

        // Email değiştiyse, başka kullanıcıda var mı kontrol et
        if (user.Email != request.Email)
        {
            var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (existingUser != null && existingUser.Id != request.UserId)
            {
                throw new BadRequestException("Bu email adresi başka bir kullanıcı tarafından kullanılıyor");
            }
        }

        // Kullanıcı bilgilerini güncelle
        user.Email = request.Email;
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        // Rolleri güncelle
        if (request.Roles != null && request.Roles.Any())
        {
            // Mevcut rolleri temizle
            user.UserRoles?.Clear();

            // Yeni rolleri ekle
            foreach (var roleName in request.Roles)
            {
                var role = await _roleRepository.GetByNameAsync(roleName, cancellationToken);
                if (role != null)
                {
                    user.UserRoles?.Add(new Domain.Entities.Authentication.UserRole
                    {
                        UserId = user.Id,
                        RoleId = role.Id
                    });
                }
            }
        }

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Kullanıcı güncellendi: {UserId}", request.UserId);

        return true;
    }
}