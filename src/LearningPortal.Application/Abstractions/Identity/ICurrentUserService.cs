namespace LearningPortal.Application.Abstractions.Identity;

/// <summary>
/// Provides the authenticated user's identity without coupling application code to HTTP.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the authenticated user's identifier, or <see langword="null"/> when unavailable.
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Gets a value indicating whether the current principal is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }
}
