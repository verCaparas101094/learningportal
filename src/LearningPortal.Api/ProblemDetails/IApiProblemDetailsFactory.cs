using LearningPortal.Shared.Results;
using Microsoft.AspNetCore.Mvc;

namespace LearningPortal.Api.ProblemDetails;

/// <summary>
/// Creates consistent RFC 7807 responses at the API boundary.
/// </summary>
public interface IApiProblemDetailsFactory
{
    /// <summary>
    /// Creates problem details for the specified application error and request.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="error">The transport-independent application error.</param>
    /// <returns>A populated RFC 7807 problem details document.</returns>
    Microsoft.AspNetCore.Mvc.ProblemDetails Create(HttpContext httpContext, Error error);
}
