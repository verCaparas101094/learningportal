using LearningPortal.Shared.Results;

namespace LearningPortal.Api.Extensions;

/// <summary>Maps transport-independent results to HTTP responses.</summary>
public static class ResultExtensions
{
    /// <summary>Converts a result into either an OK response or Problem Details.</summary>
    public static IResult ToHttpResult<T>(this Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToProblem();

    /// <summary>Converts an error into an RFC 7807 response.</summary>
    public static IResult ToProblem(this Error error)
    {
        var statusCode = error.ErrorType switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Unexpected => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(
            statusCode: statusCode,
            title: error.Message,
            type: $"https://httpstatuses.com/{statusCode}",
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}
