namespace LearningPortal.Infrastructure.Identity;

/// <summary>
/// Generates cryptographically secure refresh tokens and one-way hashes them for persistence.
/// </summary>
public interface IRefreshTokenProtector
{
    /// <summary>Generates new raw and hashed refresh-token material.</summary>
    GeneratedRefreshToken Generate();

    /// <summary>Computes the stable one-way hash for a raw refresh token.</summary>
    string Hash(string rawToken);
}

/// <summary>
/// Contains short-lived refresh-token material used while issuing a response.
/// </summary>
/// <param name="RawToken">The opaque token returned to the caller.</param>
/// <param name="TokenHash">The one-way hash stored in the database.</param>
public sealed record GeneratedRefreshToken(string RawToken, string TokenHash);
