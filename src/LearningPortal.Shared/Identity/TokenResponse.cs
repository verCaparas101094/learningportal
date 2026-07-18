namespace LearningPortal.Shared.Identity;

/// <summary>Represents a successfully issued bearer token.</summary>
/// <param name="AccessToken">The signed JWT access token.</param>
/// <param name="ExpiresAtUtc">The token expiry in UTC.</param>
public sealed record TokenResponse(string AccessToken, DateTimeOffset ExpiresAtUtc);
