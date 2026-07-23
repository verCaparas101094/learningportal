namespace LearningPortal.Infrastructure.Identity;

/// <summary>
/// Generates signed JWT access tokens for Identity users.
/// </summary>
public interface IAccessTokenGenerator
{
    /// <summary>Generates a signed access token for the specified user.</summary>
    Task<GeneratedAccessToken> GenerateAsync(
        ApplicationUser user,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Contains a generated JWT and its UTC expiration timestamp.
/// </summary>
/// <param name="Token">The serialized signed JWT.</param>
/// <param name="ExpiresAtUtc">The access token expiration timestamp.</param>
public sealed record GeneratedAccessToken(string Token, DateTimeOffset ExpiresAtUtc);
