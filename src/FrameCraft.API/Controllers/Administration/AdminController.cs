using FrameCraft.Application.Common.Models;
using FrameCraft.Application.Tenants.Commands.CreateTenant;
using FrameCraft.Application.Tenants.Commands.DeleteTenant;
using FrameCraft.Application.Tenants.Commands.UpdateTenant;
using FrameCraft.Application.Tenants.DTOs;
using FrameCraft.Application.Tenants.Queries.GetTenantById;
using FrameCraft.Application.Tenants.Queries.GetTenants;
using FrameCraft.Application.Users.Commands.CreateUser;
using FrameCraft.Application.Users.Commands.DeleteUser;
using FrameCraft.Application.Users.Commands.UpdatePassword;
using FrameCraft.Application.Users.Commands.UpdateUser;
using FrameCraft.Application.Users.DTOs;
using FrameCraft.Application.Users.Queries.GetUserById;
using FrameCraft.Application.Users.Queries.GetUsers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrameCraft.API.Controllers.Administration;

/// <summary>
/// Süper Admin işlemleri
/// Sadece IsSuperAdmin = true olan kullanıcılar erişebilir
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "SuperAdminOnly")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IMediator mediator, ILogger<AdminController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    #region Tenant İşlemleri

    /// <summary>
    /// Tüm tenant'ları listele
    /// </summary>
    [HttpGet("tenants")]
    [ProducesResponseType(typeof(ApiResponse<List<TenantSummaryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<List<TenantSummaryDto>>>> GetTenants([FromQuery] GetTenantsQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(ApiResponse<List<TenantSummaryDto>>.SuccessResult(result, "Tenant listesi alındı"));
    }

    /// <summary>
    /// Tenant detayını getir
    /// </summary>
    [HttpGet("tenants/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TenantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TenantDto>>> GetTenantById(Guid id)
    {
        var result = await _mediator.Send(new GetTenantByIdQuery { Id = id });
        return Ok(ApiResponse<TenantDto>.SuccessResult(result, "Tenant detayı alındı"));
    }

    /// <summary>
    /// Yeni tenant oluştur
    /// </summary>
    [HttpPost("tenants")]
    [ProducesResponseType(typeof(ApiResponse<TenantDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<TenantDto>>> CreateTenant([FromBody] CreateTenantCommand command)
    {
        var result = await _mediator.Send(command);

        _logger.LogInformation("Yeni tenant oluşturuldu: {TenantName} by SuperAdmin", result.Name);

        return CreatedAtAction(
            nameof(GetTenantById),
            new { id = result.Id },
            ApiResponse<TenantDto>.SuccessResult(result, "Tenant başarıyla oluşturuldu"));
    }

    /// <summary>
    /// Tenant bilgilerini güncelle
    /// </summary>
    [HttpPut("tenants/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse>> UpdateTenant(Guid id, [FromBody] UpdateTenantCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);

        _logger.LogInformation("Tenant güncellendi: {TenantId} by SuperAdmin", id);

        return Ok(ApiResponse.SuccessResult("Tenant başarıyla güncellendi"));
    }

    /// <summary>
    /// Tenant'ı sil (soft delete)
    /// </summary>
    [HttpDelete("tenants/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse>> DeleteTenant(Guid id)
    {
        var command = new DeleteTenantCommand { Id = id };
        await _mediator.Send(command);

        _logger.LogInformation("Tenant silindi: {TenantId} by SuperAdmin", id);

        return Ok(ApiResponse.SuccessResult("Tenant başarıyla silindi"));
    }

    #endregion

    #region User İşlemleri

    /// <summary>
    /// Kullanıcıları listele
    /// </summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(ApiResponse<List<UserSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<UserSummaryDto>>>> GetUsers([FromQuery] GetUsersQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(ApiResponse<List<UserSummaryDto>>.SuccessResult(result, "Kullanıcı listesi alındı"));
    }

    /// <summary>
    /// Kullanıcı detayını getir
    /// </summary>
    [HttpGet("users/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUserById(Guid id)
    {
        var query = new GetUserByIdQuery { UserId = id };
        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFound(new ErrorResponse { Message = "Kullanıcı bulunamadı" });
        }

        return Ok(ApiResponse<UserDto>.SuccessResult(result, "Kullanıcı detayı alındı"));
    }

    /// <summary>
    /// Kullanıcı bilgilerini güncelle
    /// </summary>
    [HttpPut("users/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse>> UpdateUser(Guid id, [FromBody] UpdateUserCommand command)
    {
        command.UserId = id;
        await _mediator.Send(command);

        _logger.LogInformation("Kullanıcı güncellendi: {UserId} by SuperAdmin", id);

        return Ok(ApiResponse.SuccessResult("Kullanıcı başarıyla güncellendi"));
    }

    /// <summary>
    /// Kullanıcıyı sil (soft delete)
    /// </summary>
    [HttpDelete("users/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse>> DeleteUser(Guid id)
    {
        var command = new DeleteUserCommand { UserId = id };
        await _mediator.Send(command);

        _logger.LogInformation("Kullanıcı silindi: {UserId} by SuperAdmin", id);

        return Ok(ApiResponse.SuccessResult("Kullanıcı başarıyla silindi"));
    }

    /// <summary>
    /// Yeni kullanıcı oluştur
    /// </summary>
    [HttpPost("users")]
    [ProducesResponseType(typeof(ApiResponse<CreateUserResultDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CreateUserResultDto>>> CreateUser([FromBody] CreateUserCommand command)
    {
        var result = await _mediator.Send(command);

        _logger.LogInformation("Yeni kullanıcı oluşturuldu: {Email} by SuperAdmin", result.Email);

        return Created(
            $"/api/admin/users/{result.Id}",
            ApiResponse<CreateUserResultDto>.SuccessResult(result, "Kullanıcı başarıyla oluşturuldu"));
    }

    /// <summary>
    /// Kullanıcı şifresini sıfırla (admin reset)
    /// </summary>
    [HttpPost("users/{userId:guid}/reset-password")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> ResetPassword(Guid userId, [FromBody] AdminResetPasswordRequest request)
    {
        var command = new UpdatePasswordCommand
        {
            UserId = userId,
            NewPassword = request.NewPassword,
            ConfirmPassword = request.ConfirmPassword,
            IsAdminReset = true
        };

        await _mediator.Send(command);

        _logger.LogInformation("Kullanıcı şifresi sıfırlandı: {UserId} by SuperAdmin", userId);

        return Ok(ApiResponse.SuccessResult("Şifre başarıyla sıfırlandı"));
    }

    #endregion
}

/// <summary>
/// Admin şifre sıfırlama isteği
/// </summary>
public class AdminResetPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}