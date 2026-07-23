using System.Net;

namespace LearningPortal.Blazor.Services;

/// <summary>Represents a safe RFC 7807 API failure.</summary>
public sealed class ApiProblemException(
    HttpStatusCode statusCode,
    string message,
    string? errorCode = null)
    : HttpRequestException(message, null, statusCode)
{
    /// <summary>Gets the stable API error code when supplied.</summary>
    public string? ErrorCode { get; } = errorCode;
}
