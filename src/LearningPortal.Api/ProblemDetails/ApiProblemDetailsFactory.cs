using System.Diagnostics;
using LearningPortal.Api.Constants;
using LearningPortal.Shared.Results;

namespace LearningPortal.Api.ProblemDetails;

/// <summary>
/// Creates safe and consistently structured RFC 7807 problem details documents.
/// </summary>
public sealed class ApiProblemDetailsFactory : IApiProblemDetailsFactory
{
    /// <inheritdoc />
    public Microsoft.AspNetCore.Mvc.ProblemDetails Create(HttpContext httpContext, Error error)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(error);

        var status = GetStatusCode(error.ErrorType);
        var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = status,
            Title = GetTitle(status),
            Type = $"https://httpstatuses.com/{status}",
            Detail = error.Message,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        problemDetails.Extensions["correlationId"] = GetCorrelationId(httpContext);

        if (!string.IsNullOrWhiteSpace(error.Code))
        {
            problemDetails.Extensions["errorCode"] = error.Code;
        }

        return problemDetails;
    }

    private static int GetStatusCode(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.Failure => StatusCodes.Status500InternalServerError,
        ErrorType.Unexpected => StatusCodes.Status500InternalServerError,
        _ => StatusCodes.Status500InternalServerError
    };

    private static string GetTitle(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Bad Request",
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status403Forbidden => "Forbidden",
        StatusCodes.Status404NotFound => "Not Found",
        StatusCodes.Status409Conflict => "Conflict",
        _ => "Internal Server Error"
    };

    private static string GetCorrelationId(HttpContext httpContext) =>
        httpContext.Items.TryGetValue(CorrelationIdConstants.ItemKey, out var value) && value is string correlationId
            ? correlationId
            : httpContext.TraceIdentifier;
}
