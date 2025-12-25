namespace FrameCraft.Domain.Constants;

public static class SystemConstants
{
    /// <summary>
    /// System Tenant için sabit GUID.
    /// SuperAdmin kullanıcıları bu tenant'a bağlıdır.
    /// Bu değer asla değişmemelidir.
    /// </summary>
    public static readonly Guid SystemTenantId =
        Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <summary>
    /// System Tenant adı
    /// </summary>
    public const string SystemTenantName = "System";

    /// <summary>
    /// System Tenant subdomain
    /// </summary>
    public const string SystemTenantSubdomain = "system";
}