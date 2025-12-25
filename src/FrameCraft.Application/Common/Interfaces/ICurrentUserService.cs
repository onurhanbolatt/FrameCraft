namespace FrameCraft.Application.Common.Interfaces;

/// <summary>
/// Provides information about the currently authenticated user
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user's ID
    /// </summary>
    Guid? UserId { get; }
    
    /// <summary>
    /// Gets the current user's email
    /// </summary>
    string? Email { get; }
    
    /// <summary>
    /// Gets the current user's username
    /// </summary>
    string? Username { get; }
    
    /// <summary>
    /// Checks if the current user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }
}
