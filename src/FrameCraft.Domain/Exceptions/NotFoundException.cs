namespace FrameCraft.Domain.Exceptions;

/// <summary>
/// Not Found exception (404)
/// Entity bulunamadığında fırlatılır
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string name, object key)
        : base($"{name} bulunamadı. ID: {key}")
    {
    }
}
