using LearningPortal.Api.Constants;

namespace LearningPortal.Api.Exceptions;

/// <summary>
/// Represents an expected failure caused by a requested resource not being found.
/// </summary>
public sealed class NotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class.
    /// </summary>
    /// <param name="message">The safe error message returned to the caller.</param>
    /// <param name="errorCode">The stable, machine-readable error code.</param>
    public NotFoundException(string message, string errorCode = ExceptionErrorCodes.NotFound)
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
