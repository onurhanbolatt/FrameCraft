namespace FrameCraft.Domain.Exceptions;

/// <summary>
/// 400 Bad Request - Validation hatası
/// FluentValidation hataları için kullanılır
/// </summary>
public class ValidationException : Exception
{
    /// <summary>
    /// Validation hataları (field: [errors])
    /// </summary>
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException()
        : base("Bir veya daha fazla validation hatası oluştu")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IDictionary<string, string[]> errors)
        : this()
    {
        Errors = errors;
    }

    public ValidationException(string message)
        : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
        Errors = new Dictionary<string, string[]>();
    }
}