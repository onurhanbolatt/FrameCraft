namespace FrameCraft.Domain.Entities.Authentication;

/// <summary>
/// User-Role many-to-many join table
/// Bir kullanıcının birden fazla rolü olabilir
/// </summary>
public class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
