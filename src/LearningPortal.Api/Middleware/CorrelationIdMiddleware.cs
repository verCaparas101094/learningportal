using LearningPortal.Api.Constants;

namespace LearningPortal.Api.Middleware;

/// <summary>
/// Establishes and propagates a correlation identifier for every HTTP request.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Processes a request and makes its correlation identifier available to downstream components.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <returns>A task representing the asynchronous middleware operation.</returns>
    public async Task InvokeAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var correlationId = GetOrCreateCorrelationId(httpContext);
        httpContext.Items[CorrelationIdConstants.ItemKey] = correlationId;
        httpContext.Response.OnStarting(() =>
        {
            httpContext.Response.Headers[CorrelationIdConstants.HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        await next(httpContext);
    }

    private static string GetOrCreateCorrelationId(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue(CorrelationIdConstants.HeaderName, out var values))
        {
            var incomingCorrelationId = values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
            if (incomingCorrelationId is not null)
            {
                return incomingCorrelationId.Trim();
            }
        }

        return Guid.CreateVersion7().ToString();
    }
}
