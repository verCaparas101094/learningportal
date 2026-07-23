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
    /// Gets the authenticated user's display name, or <see langword="null"/> when unavailable.
    /// </summary>
    string? DisplayName { get; }

    /// <summary>
    /// Gets the authenticated user's email address, or <see langword="null"/> when unavailable.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Gets the authenticated user's distinct assigned roles.
    /// </summary>
    IReadOnlyCollection<string> Roles { get; }

    /// <summary>
    /// Gets a value indicating whether the current principal is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Determines whether the current user belongs to the specified application role.
    /// </summary>
    /// <param name="role">The application role to test.</param>
    /// <returns><see langword="true"/> when the current user has the role.</returns>
    bool HasRole(string role);

    /// <summary>
    /// Determines whether the current user has a claim and, optionally, a specific value.
    /// </summary>
    /// <param name="claimType">The claim type to find.</param>
    /// <param name="claimValue">An optional claim value to match.</param>
    /// <returns><see langword="true"/> when a matching claim exists.</returns>
    bool HasClaim(string claimType, string? claimValue = null);
}
