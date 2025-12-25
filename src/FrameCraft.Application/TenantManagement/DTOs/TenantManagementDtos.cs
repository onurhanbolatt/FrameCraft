namespace FrameCraft.Application.TenantManagement.DTOs;

/// <summary>
/// Tenant kullanıcı bilgisi
/// </summary>
public class TenantUserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

/// <summary>
/// Tenant bilgisi
/// </summary>
public class TenantInfoDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? SubscriptionPlan { get; set; }
    public int MaxUsers { get; set; }
    public int CurrentUserCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Tenant Admin için kullanıcı oluşturma isteği
/// </summary>
public class CreateTenantUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public List<string>? Roles { get; set; }
}

/// <summary>
/// Kullanıcı durum güncelleme
/// </summary>
public class UpdateUserStatusRequest
{
    public bool IsActive { get; set; }
}

/// <summary>
/// Şifre sıfırlama isteği
/// </summary>
public class TenantResetPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Tenant kullanıcı güncelleme isteği
/// </summary>
public class UpdateTenantUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<string>? Roles { get; set; }
}