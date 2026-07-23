namespace LearningPortal.Shared.UserManagement;

/// <summary>
/// Represents the administrator-safe projection of an Identity user.
/// </summary>
/// <param name="Id">The user identifier.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="DisplayName">The user's display name.</param>
/// <param name="IsEnabled">Whether the user may authenticate.</param>
/// <param name="Roles">The user's distinct assigned roles.</param>
public sealed record UserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsEnabled,
    IReadOnlyCollection<string> Roles);
