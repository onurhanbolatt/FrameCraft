using FrameCraft.Domain.Exceptions;
using FrameCraft.Domain.Repositories.Authentication;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FrameCraft.Application.Authentication.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        ILogger<LogoutCommandHandler> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _logger = logger;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var refreshToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);
        
        if (refreshToken == null)
        {
            throw new NotFoundException("Refresh token bulunamadı");
        }

        await _refreshTokenRepository.RevokeAsync(refreshToken, cancellationToken);

        _logger.LogInformation("User logged out: {UserId}", refreshToken.UserId);
    }
}
