using MediatR;

namespace FrameCraft.Application.Users.Commands.UpdateUser;

public class UpdateUserCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = new();
}