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
        string securityStampHash,
        string createdByIp,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        Id = Guid.CreateVersion7();
        UserId = userId;
        TokenHash = tokenHash;
        SecurityStampHash = securityStampHash;
        CreatedByIp = createdByIp;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    /// <summary>Gets the persistence identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the owning Identity user identifier.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Gets the SHA-256 hash of the refresh token.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>Gets the hash of the user's security stamp at issuance.</summary>
    public string SecurityStampHash { get; private set; } = string.Empty;

    /// <summary>Gets the token creation timestamp in UTC.</summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>Gets the token expiration timestamp in UTC.</summary>
    public DateTimeOffset ExpiresAtUtc { get; private set; }

    /// <summary>Gets the token revocation timestamp in UTC.</summary>
    public DateTimeOffset? RevokedAtUtc { get; private set; }

    /// <summary>Gets the hash of the token that replaced this token during rotation.</summary>
    public string? ReplacedByTokenHash { get; private set; }

    /// <summary>Gets the client IP address that created the token.</summary>
    public string CreatedByIp { get; private set; } = string.Empty;

    /// <summary>Gets the client IP address that revoked or rotated the token.</summary>
    public string? RevokedByIp { get; private set; }

    /// <summary>Gets the SQL Server rowversion used to prevent concurrent rotation.</summary>
    public byte[] RowVersion { get; private set; } = [];

    /// <summary>Creates a persisted refresh-token record from a one-way token hash.</summary>
    public static RefreshToken Create(
        Guid userId,
        string tokenHash,
        string securityStampHash,
        string createdByIp,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user identifier is required.", nameof(userId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(securityStampHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdByIp);

        if (expiresAtUtc <= createdAtUtc)
        {
            throw new ArgumentOutOfRangeException(nameof(expiresAtUtc), "Expiration must follow creation.");
        }

        return new RefreshToken(
            userId,
            tokenHash,
            securityStampHash,
            createdByIp,
            createdAtUtc,
            expiresAtUtc);
    }

    /// <summary>Determines whether the token has expired at the supplied UTC timestamp.</summary>
    public bool IsExpired(DateTimeOffset utcNow) => ExpiresAtUtc <= utcNow;

    /// <summary>Gets a value indicating whether the token has been revoked.</summary>
    public bool IsRevoked => RevokedAtUtc is not null;

    /// <summary>Determines whether the token can currently be used.</summary>
    public bool IsActive(DateTimeOffset utcNow) =>
        !IsRevoked && !IsExpired(utcNow);

    /// <summary>Revokes the token without assigning a replacement.</summary>
    public void Revoke(DateTimeOffset revokedAtUtc, string revokedByIp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(revokedByIp);

        if (RevokedAtUtc is null)
        {
            RevokedAtUtc = revokedAtUtc;
            RevokedByIp = revokedByIp;
        }
    }

    /// <summary>Revokes the token and records the hash of its rotation replacement.</summary>
    public void Rotate(
        string replacementTokenHash,
        DateTimeOffset revokedAtUtc,
        string revokedByIp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replacementTokenHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(revokedByIp);

        if (RevokedAtUtc is not null)
        {
            return;
        }

        ReplacedByTokenHash = replacementTokenHash;
        RevokedAtUtc = revokedAtUtc;
        RevokedByIp = revokedByIp;
    }
}
