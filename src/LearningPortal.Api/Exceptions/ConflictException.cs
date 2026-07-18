using LearningPortal.Api.Constants;

namespace LearningPortal.Api.Exceptions;

/// <summary>
/// Represents an expected failure caused by a conflict with current resource state.
/// </summary>
public sealed class ConflictException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictException"/> class.
    /// </summary>
    /// <param name="message">The safe error message returned to the caller.</param>
    /// <param name="errorCode">The stable, machine-readable error code.</param>
    public ConflictException(string message, string errorCode = ExceptionErrorCodes.Conflict)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Gets the stable, machine-readable error code.
    /// </summary>
    public string ErrorCode { get; }
}
