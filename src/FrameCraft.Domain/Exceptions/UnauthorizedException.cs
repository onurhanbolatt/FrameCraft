namespace FrameCraft.Domain.Exceptions;

/// <summary>
/// 401 Unauthorized - Kimlik doğrulama hatası
/// Token geçersiz, expire olmuş veya revoke edilmiş
/// </summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message)
    {
    }

    public UnauthorizedException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
