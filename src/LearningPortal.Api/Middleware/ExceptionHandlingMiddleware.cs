using System.ComponentModel.DataAnnotations;
using LearningPortal.Api.Constants;
using LearningPortal.Api.Exceptions;
using LearningPortal.Api.ProblemDetails;
using LearningPortal.Shared.Results;

namespace LearningPortal.Api.Middleware;

/// <summary>
/// Converts unhandled exceptions into safe RFC 7807 responses at the API boundary.
/// </summary>
public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    /// <summary>
    /// Executes the downstream pipeline and handles any unhandled exception.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="problemDetailsFactory">The centralized problem details factory.</param>
    /// <returns>A task representing the asynchronous middleware operation.</returns>
    public async Task InvokeAsync(
        HttpContext httpContext,
        IApiProblemDetailsFactory problemDetailsFactory)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(problemDetailsFactory);

        try
        {
            await next(httpContext);
        }
        catch (Exception exception)
        {
            if (httpContext.Response.HasStarted)
            {
                logger.LogError(
                    exception,
                    "An exception occurred after the response started for {Method} {Path}.",
                    httpContext.Request.Method,
                    httpContext.Request.Path);
                throw;
            }

            await HandleExceptionAsync(httpContext, exception, problemDetailsFactory);
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext httpContext,
        Exception exception,
        IApiProblemDetailsFactory problemDetailsFactory)
    {
        var error = MapException(exception);
        var correlationId = httpContext.Items[CorrelationIdConstants.ItemKey] as string;

        if (error.ErrorType is ErrorType.Unexpected or ErrorType.Failure)
        {
            logger.LogError(
                exception,
                "Unhandled exception for {Method} {Path}. CorrelationId: {CorrelationId}.",
                httpContext.Request.Method,
                httpContext.Request.Path,
                correlationId);
        }
        else
        {
            logger.LogWarning(
                exception,
                "Request failed for {Method} {Path} with {ErrorCode}. CorrelationId: {CorrelationId}.",
                httpContext.Request.Method,
                httpContext.Request.Path,
                error.Code,
                correlationId);
        }

        var problemDetails = problemDetailsFactory.Create(httpContext, error);
        httpContext.Response.Clear();
        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: CancellationToken.None);
    }

    private static Error MapException(Exception exception) => exception switch
    {
        ValidationException validationException => new Error(
            ExceptionErrorCodes.Validation,
            validationException.Message,
            ErrorType.Validation),
        NotFoundException notFoundException => new Error(
            notFoundException.ErrorCode,
            notFoundException.Message,
            ErrorType.NotFound),
        ConflictException conflictException => new Error(
            conflictException.ErrorCode,
            conflictException.Message,
            ErrorType.Conflict),
        UnauthorizedAccessException => Errors.Authentication.Unauthorized(),
        ForbiddenAccessException forbiddenException => new Error(
            forbiddenException.ErrorCode,
            forbiddenException.Message,
            ErrorType.Forbidden),
        OperationCanceledException => new Error(
            ExceptionErrorCodes.Cancelled,
            "The request was cancelled.",
            ErrorType.Validation),
        _ => Errors.Common.Unexpected()
    };
}
