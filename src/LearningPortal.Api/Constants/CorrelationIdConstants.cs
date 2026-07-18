namespace LearningPortal.Api.Constants;

/// <summary>
/// Defines names used to propagate correlation identifiers through HTTP requests.
/// </summary>
public static class CorrelationIdConstants
{
    /// <summary>
    /// Gets the HTTP header used to receive and return a correlation identifier.
    /// </summary>
    public const string HeaderName = "X-Correlation-ID";

    /// <summary>
    /// Gets the <see cref="HttpContext.Items"/> key used to store the current correlation identifier.
    /// </summary>
    public const string ItemKey = "LearningPortal.CorrelationId";
}
