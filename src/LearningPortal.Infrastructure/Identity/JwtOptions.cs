namespace LearningPortal.Infrastructure.Identity;

/// <summary>Contains settings used to validate and issue JWT access tokens.</summary>
public sealed class JwtOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Jwt";

    /// <summary>Gets or sets the expected token issuer.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Gets or sets the expected token audience.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Gets or sets the symmetric signing key.</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the token lifetime in minutes.</summary>
    public int ExpirationMinutes { get; set; } = 60;

    /// <summary>Gets or sets the refresh token lifetime in days.</summary>
    public int RefreshTokenExpirationDays { get; set; } = 30;
}
