using FrameCraft.Application.Users.DTOs;
using MediatR;

namespace FrameCraft.Application.Users.Commands.CreateUser;

/// <summary>
/// Yeni kullanıcı oluşturma komutu
/// SuperAdmin: Herhangi bir tenant'a kullanıcı ekleyebilir
/// Admin: Sadece kendi tenant'ına kullanıcı ekleyebilir
/// </summary>
public record CreateUserCommand : IRequest<CreateUserResultDto>
{
    /// <summary>
    /// Kullanıcının ait olacağı tenant ID
    /// SuperAdmin için zorunlu, Admin için otomatik atanır
    /// </summary>
    public Guid TenantId { get; init; }
    
    /// <summary>
    /// E-posta adresi (benzersiz)
    /// </summary>
    public string Email { get; init; } = string.Empty;
    
    /// <summary>
    /// Şifre (min 6 karakter)
    /// </summary>
    public string Password { get; init; } = string.Empty;
    
    /// <summary>
    /// Ad
    /// </summary>
    public string FirstName { get; init; } = string.Empty;
    
    /// <summary>
    /// Soyad
    /// </summary>
    public string LastName { get; init; } = string.Empty;
    
    /// <summary>
    /// Roller (örn: ["Admin", "Cashier"])
    /// </summary>
    public List<string> Roles { get; init; } = new();
    
    /// <summary>
    /// SuperAdmin mi? (Sadece SuperAdmin oluşturabilir)
    /// </summary>
    public bool IsSuperAdmin { get; init; } = false;
}
