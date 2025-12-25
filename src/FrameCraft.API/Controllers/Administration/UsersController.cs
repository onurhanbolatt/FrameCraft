using FrameCraft.Application.Common.Models;
using FrameCraft.Application.Users.Commands.UpdatePassword;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FrameCraft.API.Controllers.Administration;

/// <summary>
/// Kullanıcı işlemleri
/// Her kullanıcı kendi bilgilerini yönetebilir
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IMediator mediator, ILogger<UsersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Mevcut kullanıcı bilgilerini getir
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<CurrentUserDto>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<CurrentUserDto>> GetCurrentUser()
    {
        var userId = GetCurrentUserId();
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
        var fullName = User.FindFirst("FullName")?.Value ?? string.Empty;
        var tenantId = User.FindFirst("TenantId")?.Value;
        var isSuperAdmin = bool.TryParse(User.FindFirst("IsSuperAdmin")?.Value, out var sa) && sa;
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        var currentUser = new CurrentUserDto
        {
            Id = userId,
            Email = email,
            FullName = fullName,
            TenantId = Guid.TryParse(tenantId, out var tid) ? tid : Guid.Empty,
            IsSuperAdmin = isSuperAdmin,
            Roles = roles
        };

        return Ok(ApiResponse<CurrentUserDto>.SuccessResult(currentUser));
    }

    /// <summary>
    /// Şifre değiştir
    /// </summary>
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetCurrentUserId();

        var command = new UpdatePasswordCommand
        {
            UserId = userId,
            CurrentPassword = request.CurrentPassword,
            NewPassword = request.NewPassword,
            ConfirmPassword = request.ConfirmPassword,
            IsAdminReset = false
        };

        await _mediator.Send(command);
        
        _logger.LogInformation("Kullanıcı şifresini değiştirdi: {UserId}", userId);

        return Ok(ApiResponse.SuccessResult("Şifre başarıyla değiştirildi"));
    }

    #region Helper Methods

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    #endregion
}

/// <summary>
/// Mevcut kullanıcı bilgileri
/// </summary>
public class CurrentUserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public bool IsSuperAdmin { get; set; }
    public List<string> Roles { get; set; } = new();
}

/// <summary>
/// Şifre değiştirme isteği
/// </summary>
public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
