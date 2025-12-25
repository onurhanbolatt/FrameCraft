using System.Security.Claims;
using FrameCraft.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace FrameCraft.Infrastructure.Services.Identity;

/// <summary>
/// Provides information about the currently authenticated user from HTTP context
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");
            
            return Guid.TryParse(userId, out var id) ? id : null;
        }
    }

    public string? Email => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);

    public string? Username => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name)
                               ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("username");

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
