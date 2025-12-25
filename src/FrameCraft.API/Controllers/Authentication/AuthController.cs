using FrameCraft.Application.Authentication.Commands.Login;
using FrameCraft.Application.Authentication.Commands.Logout;
using FrameCraft.Application.Authentication.Commands.RefreshToken;
using FrameCraft.Application.Authentication.DTOs;
using FrameCraft.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrameCraft.API.Controllers.Authentication;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Kullanıcı girişi - Email ve şifre ile JWT token alın
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login([FromBody] LoginCommand command)
    {
        try
        {
            // IP adresini ekle
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var commandWithIp = command with { IpAddress = ipAddress };

            var result = await _mediator.Send(commandWithIp);

            _logger.LogInformation("Başarılı login: {Email} from {IpAddress}", command.Email, ipAddress);

            return Ok(ApiResponse<LoginResponseDto>.SuccessResult(result, "Giriş başarılı"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Login başarısız: {Email}", command.Email);
            return BadRequest(new ErrorResponse
            {
                StatusCode = 400,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Token yenileme - Refresh token ile yeni access token alın
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> RefreshToken([FromBody] RefreshTokenCommand command)
    {
        try
        {
            // IP adresini ekle
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var commandWithIp = command with { IpAddress = ipAddress };

            var result = await _mediator.Send(commandWithIp);

            _logger.LogInformation("Token yenilendi - User: {UserId} from {IpAddress}", result.UserId, ipAddress);

            return Ok(ApiResponse<LoginResponseDto>.SuccessResult(result, "Token başarıyla yenilendi"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token yenileme başarısız");
            return Unauthorized(new ErrorResponse
            {
                StatusCode = 401,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Çıkış - Refresh token'ı geçersiz kıl
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> Logout([FromBody] LogoutCommand command)
    {
        try
        {
            await _mediator.Send(command);

            _logger.LogInformation("Başarılı logout");

            return Ok(ApiResponse.SuccessResult("Çıkış başarılı"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Logout başarısız");
            return NotFound(new ErrorResponse
            {
                StatusCode = 404,
                Message = ex.Message
            });
        }
    }
}
