using FrameCraft.Domain.Entities.Common;

namespace FrameCraft.Domain.Entities.Authentication;

/// <summary>
/// Rol entity'si
/// Admin, User, Manager gibi roller
/// </summary>
public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Navigation Properties
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
