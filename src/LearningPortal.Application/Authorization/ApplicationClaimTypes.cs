namespace LearningPortal.Application.Authorization;

/// <summary>
/// Defines stable claim names shared by token generation and authorization consumers.
/// </summary>
public static class ApplicationClaimTypes
{
    /// <summary>Identifies the user and maps to the JWT subject claim.</summary>
    public const string UserId = "sub";

    /// <summary>Contains the user's display name.</summary>
    public const string DisplayName = "name";

    /// <summary>Contains the user's email address.</summary>
    public const string Email = "email";

    /// <summary>Contains one application role per claim.</summary>
    public const string Role = "role";

    /// <summary>Reserved for future permission-based authorization.</summary>
    public const string Permission = "permission";

    /// <summary>Identifies an individual JWT.</summary>
    public const string JwtId = "jti";

    /// <summary>Contains the JWT issue time as a NumericDate value.</summary>
    public const string IssuedAt = "iat";
}
