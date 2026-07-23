namespace LearningPortal.Shared.Identity;

/// <summary>
/// Represents a successfully issued access and refresh token pair.
/// </summary>
/// <param name="AccessToken">The signed JWT access token.</param>
/// <param name="AccessTokenExpiresAtUtc">The access token expiry in UTC.</param>
/// <param name="RefreshToken">The opaque refresh token returned only to the caller.</param>
/// <param name="RefreshTokenExpiresAtUtc">The refresh token expiry in UTC.</param>
public sealed record AuthenticationResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);
