using FrameCraft.Domain.Enums;
using MediatR;

namespace FrameCraft.Application.Tenants.Commands.UpdateTenant;

public class UpdateTenantCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public TenantStatus Status { get; set; }
    public string? SubscriptionPlan { get; set; }
    public int MaxUsers { get; set; }
    public int StorageQuotaMB { get; set; }
    public DateTime? ExpiresAt { get; set; }
}