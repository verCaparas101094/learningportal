namespace LearningPortal.Shared.Results;

/// <summary>
/// Represents an immutable, transport-independent application error.
/// </summary>
/// <param name="Code">The stable, machine-readable error code.</param>
/// <param name="Message">The safe, human-readable error message.</param>
/// <param name="ErrorType">The category of the error.</param>
public sealed record Error(string Code, string Message, ErrorType ErrorType);
