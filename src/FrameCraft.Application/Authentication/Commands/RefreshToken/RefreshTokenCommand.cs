using FrameCraft.Application.Authentication.DTOs;
using MediatR;

namespace FrameCraft.Application.Authentication.Commands.RefreshToken;

/// <summary>
/// Refresh token ile yeni access token al
/// </summary>
public record RefreshTokenCommand : IRequest<LoginResponseDto>
{
    /// <summary>
    /// Refresh token (Base64 encoded)
    /// </summary>
    public string RefreshToken { get; init; } = string.Empty;

    /// <summary>
    /// İstemci IP adresi (token rotation için)
    /// </summary>
    public string? IpAddress { get; init; }
}
