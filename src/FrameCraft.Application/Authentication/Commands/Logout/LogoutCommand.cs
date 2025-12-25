using MediatR;

namespace FrameCraft.Application.Authentication.Commands.Logout;

public record LogoutCommand(string RefreshToken) : IRequest;
