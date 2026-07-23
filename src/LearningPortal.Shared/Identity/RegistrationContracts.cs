namespace LearningPortal.Shared.Identity;

/// <summary>Supplies public self-registration values without role selection.</summary>
public sealed record RegisterRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string ConfirmPassword);

/// <summary>Returns the safe authenticated-user profile.</summary>
public sealed record CurrentUserResponse(
    Guid Id,
    string DisplayName,
    string Email,
    IReadOnlyCollection<string> Roles,
    bool IsActive);
