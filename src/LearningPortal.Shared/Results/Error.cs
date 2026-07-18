namespace LearningPortal.Shared.Results;

/// <summary>Represents a stable, transport-independent application error.</summary>
/// <param name="Code">A machine-readable error code.</param>
/// <param name="Message">A safe human-readable message.</param>
/// <param name="Type">The error category.</param>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    /// <summary>Creates a validation error.</summary>
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    /// <summary>Creates an unauthorized error.</summary>
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);

    /// <summary>Creates a general failure.</summary>
    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);
}
