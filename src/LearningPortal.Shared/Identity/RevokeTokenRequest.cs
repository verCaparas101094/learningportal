namespace LearningPortal.Shared.Identity;

/// <summary>
/// Represents a request to revoke a refresh token.
/// </summary>
/// <param name="RefreshToken">The raw refresh token to revoke.</param>
public sealed record RevokeTokenRequest(string RefreshToken);
