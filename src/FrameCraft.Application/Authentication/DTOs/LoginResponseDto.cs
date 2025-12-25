namespace FrameCraft.Application.Authentication.DTOs;

/// <summary>
/// Login response DTO
/// </summary>
public class LoginResponseDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public List<string> Roles { get; set; } = new();
    public bool IsSuperAdmin { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiration { get; set; }
    public DateTime RefreshTokenExpiration { get; set; }
}
