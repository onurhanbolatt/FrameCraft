namespace FrameCraft.Domain.Exceptions;

/// <summary>
/// Bad Request exception (400)
/// Business rule ihlali veya geçersiz istek
/// </summary>
public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message)
    {
    }

    public BadRequestException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
