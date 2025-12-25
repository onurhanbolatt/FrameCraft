using FrameCraft.Application.Common.Interfaces;
using FrameCraft.Domain.Constants;
using FrameCraft.Domain.Exceptions;
using FrameCraft.Domain.Repositories;
using FrameCraft.Domain.Repositories.Authentication;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FrameCraft.Application.Users.Commands.DeleteUser;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeleteUserCommandHandler> _logger;

    public DeleteUserCommandHandler(
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        ILogger<DeleteUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user == null || user.IsDeleted)
        {
            throw new NotFoundException("Kullanıcı bulunamadı");
        }

        // Kendini silmeye çalışıyorsa engelle
        if (_currentUserService.UserId == request.UserId)
        {
            throw new BadRequestException("Kendinizi silemezsiniz");
        }

        // SuperAdmin silinmeye çalışılıyorsa engelle
        if (user.IsSuperAdmin)
        {
            throw new ForbiddenAccessException("SuperAdmin kullanıcılar silinemez");
        }

        // Soft delete
        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.IsActive = false;

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken); 

        _logger.LogInformation("Kullanıcı silindi: {UserId}", request.UserId);

        return true;
    }
}