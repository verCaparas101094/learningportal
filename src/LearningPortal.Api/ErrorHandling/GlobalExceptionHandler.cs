using Microsoft.AspNetCore.Diagnostics;

namespace LearningPortal.Api.ErrorHandling;

/// <summary>Logs unhandled exceptions and returns a safe RFC 7807 response.</summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception for {Method} {Path}.", httpContext.Request.Method, httpContext.Request.Path);

        await Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred.",
                detail: "The server could not complete the request.",
                extensions: new Dictionary<string, object?> { ["traceId"] = httpContext.TraceIdentifier })
            .ExecuteAsync(httpContext);

        return true;
    }
}
