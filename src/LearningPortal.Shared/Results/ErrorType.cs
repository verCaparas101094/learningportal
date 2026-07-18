namespace LearningPortal.Shared.Results;

/// <summary>
/// Defines transport-independent categories for application errors.
/// </summary>
public enum ErrorType
{
    /// <summary>Indicates that one or more input values are invalid.</summary>
    Validation,

    /// <summary>Indicates that a requested resource does not exist.</summary>
    NotFound,

    /// <summary>Indicates that the operation conflicts with existing state.</summary>
    Conflict,

    /// <summary>Indicates that authentication is required or has failed.</summary>
    Unauthorized,

    /// <summary>Indicates that the authenticated caller is not permitted to perform the operation.</summary>
    Forbidden,

    /// <summary>Indicates an expected application or business operation failure.</summary>
    Failure,

    /// <summary>Indicates an unexpected technical failure.</summary>
    Unexpected
}
