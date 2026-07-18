namespace LearningPortal.Shared.Results;

/// <summary>Describes the HTTP-neutral category of an application error.</summary>
public enum ErrorType
{
    /// <summary>A validation rule was violated.</summary>
    Validation,
    /// <summary>The requested resource was not found.</summary>
    NotFound,
    /// <summary>The operation conflicts with current state.</summary>
    Conflict,
    /// <summary>The caller is not authenticated.</summary>
    Unauthorized,
    /// <summary>The caller lacks permission.</summary>
    Forbidden,
    /// <summary>An unexpected failure occurred.</summary>
    Failure
}
