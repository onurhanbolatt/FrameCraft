namespace FrameCraft.Domain.Exceptions;

/// <summary>
/// Yetki hatası - 403 Forbidden
/// </summary>
public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException() : base("Bu kaynağa erişim yetkiniz yok.")
    {
    }

    public ForbiddenAccessException(string message) : base(message)
    {
    }

    public ForbiddenAccessException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
