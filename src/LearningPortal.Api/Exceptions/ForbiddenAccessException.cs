using LearningPortal.Api.Constants;

namespace LearningPortal.Api.Exceptions;

/// <summary>
/// Represents an expected failure caused by insufficient permissions.
/// </summary>
public sealed class ForbiddenAccessException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForbiddenAccessException"/> class.
    /// </summary>
    /// <param name="message">The safe error message returned to the caller.</param>
    /// <param name="errorCode">The stable, machine-readable error code.</param>
    public ForbiddenAccessException(
        string message = "You do not have permission to perform this operation.",
        string errorCode = ExceptionErrorCodes.Forbidden)
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
