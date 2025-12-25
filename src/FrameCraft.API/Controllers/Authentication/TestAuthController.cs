using FrameCraft.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FrameCraft.API.Controllers.Authentication;

[ApiController]
[Route("api/[controller]")]
public class TestAuthController : ControllerBase
{
    /// <summary>
    /// Public endpoint - Herkes erişebilir
    /// </summary>
    [HttpGet("public")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<string>> Public()
    {
        return Ok(ApiResponse<string>.SuccessResult("Bu endpoint herkese açık!"));
    }

    /// <summary>
    /// Protected endpoint - Sadece login olmuş kullanıcılar
    /// </summary>
    [HttpGet("protected")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<ApiResponse<object>> Protected()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var fullName = User.FindFirst(ClaimTypes.Name)?.Value;
        var tenantId = User.FindFirst("TenantId")?.Value;

        return Ok(ApiResponse<object>.SuccessResult(new
        {
            Message = "Bu endpoint korumalı! Sadece login olmuş kullanıcılar erişebilir.",
            UserId = userId,
            Email = email,
            FullName = fullName,
            TenantId = tenantId
        }));
    }

    /// <summary>
    /// Admin only endpoint - Sadece Admin rolü veya SuperAdmin
    /// </summary>
    [HttpGet("admin-only")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<ApiResponse<string>> AdminOnly()
    {
        return Ok(ApiResponse<string>.SuccessResult("Bu endpoint sadece Admin/SuperAdmin için!"));
    }

    /// <summary>
    /// SuperAdmin only endpoint
    /// </summary>
    [HttpGet("superadmin-only")]
    [Authorize(Policy = "SuperAdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<ApiResponse<string>> SuperAdminOnly()
    {
        return Ok(ApiResponse<string>.SuccessResult("Bu endpoint sadece SuperAdmin için!"));
    }

    /// <summary>
    /// User info - Giriş yapan kullanıcı bilgileri
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<ApiResponse<object>> Me()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();

        return Ok(ApiResponse<object>.SuccessResult(new
        {
            UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            Email = User.FindFirst(ClaimTypes.Email)?.Value,
            FullName = User.FindFirst(ClaimTypes.Name)?.Value,
            TenantId = User.FindFirst("TenantId")?.Value,
            IsSuperAdmin = User.FindFirst("IsSuperAdmin")?.Value,
            Roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList(),
            AllClaims = claims
        }));
    }
}