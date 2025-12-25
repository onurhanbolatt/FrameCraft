using FrameCraft.Application.Users.DTOs;
using MediatR;

namespace FrameCraft.Application.Users.Queries.GetUserById;

public class GetUserByIdQuery : IRequest<UserDto?>
{
    public Guid UserId { get; set; }
}