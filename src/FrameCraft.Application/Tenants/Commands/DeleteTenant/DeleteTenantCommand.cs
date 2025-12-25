using MediatR;

namespace FrameCraft.Application.Tenants.Commands.DeleteTenant;

public class DeleteTenantCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}