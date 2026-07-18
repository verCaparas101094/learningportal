namespace LearningPortal.Api.Constants;

/// <summary>
/// Defines stable error codes for exceptions mapped by the API boundary.
/// </summary>
public static class ExceptionErrorCodes
{
    /// <summary>The default validation failure code.</summary>
    public const string Validation = "Validation.Failed";

    /// <summary>The default resource-not-found code.</summary>
    public const string NotFound = "Common.NotFound";

    /// <summary>The default conflict code.</summary>
    public const string Conflict = "Common.Conflict";

    /// <summary>The authentication failure code.</summary>
    public const string Unauthorized = "Authentication.Unauthorized";

    /// <summary>The authorization failure code.</summary>
    public const string Forbidden = "Authorization.Forbidden";

    /// <summary>The request-cancellation code.</summary>
    public const string Cancelled = "Request.Cancelled";

    /// <summary>The unexpected server failure code.</summary>
    public const string Unexpected = "Common.Unexpected";
}
