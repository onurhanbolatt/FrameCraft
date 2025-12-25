using FrameCraft.Application.Common.Models;
using FrameCraft.Application.TenantManagement.Commands;
using FrameCraft.Application.TenantManagement.DTOs;
using FrameCraft.Application.TenantManagement.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FrameCraft.API.Controllers.Administration;

/// <summary>
/// Tenant Admin işlemleri
/// Dükkan sahibinin kendi çalışanlarını yönetmesi için
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class TenantManagementController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TenantManagementController> _logger;

    public TenantManagementController(IMediator mediator, ILogger<TenantManagementController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Kendi tenant'ındaki kullanıcıları listele
    /// </summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(ApiResponse<List<TenantUserDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<List<TenantUserDto>>>> GetMyTenantUsers()
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == Guid.Empty)
        {
            return BadRequest(new ErrorResponse { StatusCode = 400, Message = "Tenant bilgisi bulunamadı" });
        }

        var result = await _mediator.Send(new GetTenantUsersQuery(tenantId));
        return Ok(ApiResponse<List<TenantUserDto>>.SuccessResult(result, $"{result.Count} kullanıcı bulundu"));
    }

    /// <summary>
    /// Kendi tenant'ındaki kullanıcı detayını getir
    /// </summary>
    [HttpGet("users/{userId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TenantUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TenantUserDto>>> GetTenantUser(Guid userId)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == Guid.Empty)
        {
            return BadRequest(new ErrorResponse { StatusCode = 400, Message = "Tenant bilgisi bulunamadı" });
        }

        var result = await _mediator.Send(new GetTenantUserByIdQuery(userId, tenantId));

        if (result == null)
        {
            return NotFound(new ErrorResponse { StatusCode = 404, Message = "Kullanıcı bulunamadı" });
        }

        return Ok(ApiResponse<TenantUserDto>.SuccessResult(result));
    }

    /// <summary>
    /// Kendi tenant'ına yeni çalışan ekle
    /// </summary>
    [HttpPost("users")]
    [ProducesResponseType(typeof(ApiResponse<CreateTenantUserResult>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CreateTenantUserResult>>> CreateTenantUser([FromBody] CreateTenantUserRequest request)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == Guid.Empty)
        {
            return BadRequest(new ErrorResponse { StatusCode = 400, Message = "Tenant bilgisi bulunamadı" });
        }

        var command = new CreateTenantUserCommand
        {
            TenantId = tenantId,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Password = request.Password,
            Roles = request.Roles ?? new List<string> { "User" }
        };

        var result = await _mediator.Send(command);

        _logger.LogInformation("Tenant Admin yeni çalışan ekledi: {Email}", result.Email);

        return Created(
            $"/api/tenantmanagement/users/{result.Id}",
            ApiResponse<CreateTenantUserResult>.SuccessResult(result, "Çalışan başarıyla eklendi"));
    }

    /// <summary>
    /// Kendi tenant'ındaki kullanıcıyı aktif/pasif yap
    /// </summary>
    [HttpPut("users/{userId:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> UpdateUserStatus(Guid userId, [FromBody] UpdateUserStatusRequest request)
    {
        var tenantId = GetCurrentTenantId();
        var currentUserId = GetCurrentUserId();

        var command = new UpdateTenantUserStatusCommand
        {
            UserId = userId,
            TenantId = tenantId,
            CurrentUserId = currentUserId,
            IsActive = request.IsActive
        };

        var result = await _mediator.Send(command);

        if (!result)
        {
            return NotFound(new ErrorResponse { StatusCode = 404, Message = "Kullanıcı bulunamadı" });
        }

        var status = request.IsActive ? "aktif" : "pasif";
        return Ok(ApiResponse.SuccessResult($"Kullanıcı {status} yapıldı"));
    }

    /// <summary>
    /// Kendi tenant'ındaki kullanıcıyı güncelle
    /// </summary>
    [HttpPut("users/{userId:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse>> UpdateTenantUser(Guid userId, [FromBody] UpdateTenantUserRequest request)
    {
        var tenantId = GetCurrentTenantId();
        var currentUserId = GetCurrentUserId();

        if (tenantId == Guid.Empty)
        {
            return BadRequest(new ErrorResponse { StatusCode = 400, Message = "Tenant bilgisi bulunamadı" });
        }

        var command = new UpdateTenantUserCommand
        {
            UserId = userId,
            TenantId = tenantId,
            CurrentUserId = currentUserId,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsActive = request.IsActive,
            Roles = request.Roles ?? new List<string>()
        };

        var result = await _mediator.Send(command);

        if (!result)
        {
            return NotFound(new ErrorResponse { StatusCode = 404, Message = "Kullanıcı bulunamadı" });
        }

        _logger.LogInformation("Tenant user güncellendi: {UserId}", userId);

        return Ok(ApiResponse.SuccessResult("Kullanıcı başarıyla güncellendi"));
    }

    /// <summary>
    /// Kendi tenant'ındaki kullanıcının şifresini sıfırla
    /// </summary>
    [HttpPost("users/{userId:guid}/reset-password")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> ResetUserPassword(Guid userId, [FromBody] TenantResetPasswordRequest request)
    {
        var tenantId = GetCurrentTenantId();

        var command = new ResetTenantUserPasswordCommand
        {
            UserId = userId,
            TenantId = tenantId,
            NewPassword = request.NewPassword,
            ConfirmPassword = request.ConfirmPassword
        };

        var result = await _mediator.Send(command);

        if (!result)
        {
            return NotFound(new ErrorResponse { StatusCode = 404, Message = "Kullanıcı bulunamadı" });
        }

        return Ok(ApiResponse.SuccessResult("Şifre başarıyla sıfırlandı"));
    }

    /// <summary>
    /// Kendi tenant'ındaki kullanıcıyı sil (soft delete)
    /// </summary>
    [HttpDelete("users/{userId:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> DeleteTenantUser(Guid userId)
    {
        var tenantId = GetCurrentTenantId();
        var currentUserId = GetCurrentUserId();

        var command = new DeleteTenantUserCommand
        {
            UserId = userId,
            TenantId = tenantId,
            CurrentUserId = currentUserId
        };

        var result = await _mediator.Send(command);

        if (!result)
        {
            return NotFound(new ErrorResponse { StatusCode = 404, Message = "Kullanıcı bulunamadı" });
        }

        return Ok(ApiResponse.SuccessResult("Kullanıcı başarıyla silindi"));
    }

    /// <summary>
    /// Tenant bilgilerini getir (kendi tenant'ı)
    /// </summary>
    [HttpGet("info")]
    [ProducesResponseType(typeof(ApiResponse<TenantInfoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TenantInfoDto>>> GetTenantInfo()
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == Guid.Empty)
        {
            return BadRequest(new ErrorResponse { StatusCode = 400, Message = "Tenant bilgisi bulunamadı" });
        }

        var result = await _mediator.Send(new GetTenantInfoQuery(tenantId));

        if (result == null)
        {
            return NotFound(new ErrorResponse { StatusCode = 404, Message = "Tenant bulunamadı" });
        }

        return Ok(ApiResponse<TenantInfoDto>.SuccessResult(result));
    }

    #region Helper Methods

    private Guid GetCurrentTenantId()
    {
        var tenantIdClaim = User.FindFirst("TenantId")?.Value;
        return Guid.TryParse(tenantIdClaim, out var tenantId) ? tenantId : Guid.Empty;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    #endregion
}