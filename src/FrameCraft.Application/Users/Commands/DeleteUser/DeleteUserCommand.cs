using MediatR;

namespace FrameCraft.Application.Users.Commands.DeleteUser;

public class DeleteUserCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
}