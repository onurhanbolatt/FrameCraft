using FrameCraft.Application.Authentication.DTOs;
using MediatR;
using System.Text.Json.Serialization;

namespace FrameCraft.Application.Authentication.Commands.Login;

public record LoginCommand : IRequest<LoginResponseDto>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;

    [JsonIgnore]
    public string? IpAddress { get; init; }
}
