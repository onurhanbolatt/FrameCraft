namespace FrameCraft.Application.Common.Models;

/// <summary>
/// JWT configuration settings
/// </summary>
public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 60; // 1 saat
    public int RefreshTokenExpirationDays { get; set; } = 7; // 7 gün
}
