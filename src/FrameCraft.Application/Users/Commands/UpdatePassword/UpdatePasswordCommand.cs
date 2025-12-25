using MediatR;

namespace FrameCraft.Application.Users.Commands.UpdatePassword;

/// <summary>
/// Şifre değiştirme komutu
/// Kullanıcı kendi şifresini değiştirebilir
/// SuperAdmin herhangi bir kullanıcının şifresini değiştirebilir
/// </summary>
public record UpdatePasswordCommand : IRequest<bool>
{
    /// <summary>
    /// Şifresi değiştirilecek kullanıcı ID
    /// </summary>
    public Guid UserId { get; init; }
    
    /// <summary>
    /// Mevcut şifre (kendi şifresini değiştirirken zorunlu)
    /// SuperAdmin için gerekli değil
    /// </summary>
    public string? CurrentPassword { get; init; }
    
    /// <summary>
    /// Yeni şifre
    /// </summary>
    public string NewPassword { get; init; } = string.Empty;
    
    /// <summary>
    /// Yeni şifre tekrarı
    /// </summary>
    public string ConfirmPassword { get; init; } = string.Empty;
    
    /// <summary>
    /// Admin tarafından mı değiştiriliyor?
    /// True ise CurrentPassword kontrolü yapılmaz
    /// </summary>
    public bool IsAdminReset { get; init; } = false;
}
