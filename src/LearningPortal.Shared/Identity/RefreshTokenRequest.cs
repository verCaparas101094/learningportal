namespace LearningPortal.Shared.Identity;

/// <summary>
/// Represents a request to rotate an active refresh token.
/// </summary>
/// <param name="RefreshToken">The raw refresh token previously issued to the caller.</param>
public sealed record RefreshTokenRequest(string RefreshToken);
