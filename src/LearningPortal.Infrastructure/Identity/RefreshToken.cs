namespace LearningPortal.Infrastructure.Identity;

/// <summary>
/// Represents a persisted, hashed refresh token and its rotation state.
/// </summary>
public sealed class RefreshToken
{
    private RefreshToken()
    {
    }

    private RefreshToken(
        Guid userId,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        Id = Guid.CreateVersion7();
        UserId = userId;
        TokenHash = tokenHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    /// <summary>Gets the persistence identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the owning Identity user identifier.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Gets the SHA-256 hash of the refresh token.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>Gets the token creation timestamp in UTC.</summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>Gets the token expiration timestamp in UTC.</summary>
    public DateTimeOffset ExpiresAtUtc { get; private set; }

    /// <summary>Gets the token revocation timestamp in UTC.</summary>
    public DateTimeOffset? RevokedAtUtc { get; private set; }

    /// <summary>Gets the hash of the token that replaced this token during rotation.</summary>
    public string? ReplacedByTokenHash { get; private set; }

    /// <summary>Gets the SQL Server rowversion used to prevent concurrent rotation.</summary>
    public byte[] RowVersion { get; private set; } = [];

    /// <summary>Creates a persisted refresh-token record from a one-way token hash.</summary>
    public static RefreshToken Create(
        Guid userId,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user identifier is required.", nameof(userId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        if (expiresAtUtc <= createdAtUtc)
        {
            throw new ArgumentOutOfRangeException(nameof(expiresAtUtc), "Expiration must follow creation.");
        }

        return new RefreshToken(userId, tokenHash, createdAtUtc, expiresAtUtc);
    }

    /// <summary>Determines whether the token can currently be used.</summary>
    public bool IsActive(DateTimeOffset utcNow) =>
        RevokedAtUtc is null && ExpiresAtUtc > utcNow;

    /// <summary>Revokes the token without assigning a replacement.</summary>
    public void Revoke(DateTimeOffset revokedAtUtc)
    {
        if (RevokedAtUtc is null)
        {
            RevokedAtUtc = revokedAtUtc;
        }
    }

    /// <summary>Revokes the token and records the hash of its rotation replacement.</summary>
    public void Rotate(string replacementTokenHash, DateTimeOffset revokedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replacementTokenHash);

        if (RevokedAtUtc is not null)
        {
            return;
        }

        ReplacedByTokenHash = replacementTokenHash;
        RevokedAtUtc = revokedAtUtc;
    }
}
