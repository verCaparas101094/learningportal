using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;

namespace LearningPortal.Blazor.Services;

/// <summary>
/// Forwards the current request's JWT bearer token to Learning Portal API requests.
/// </summary>
public sealed class CurrentBearerTokenHandler(IHttpContextAccessor httpContextAccessor)
    : DelegatingHandler
{
    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Headers.Authorization is null)
        {
            var httpContext = httpContextAccessor.HttpContext;
            var authorizationHeader = httpContext?
                .Request
                .Headers
                .Authorization
                .FirstOrDefault();

            if (AuthenticationHeaderValue.TryParse(authorizationHeader, out var parsedHeader)
                && string.Equals(
                    parsedHeader.Scheme,
                    "Bearer",
                    StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(parsedHeader.Parameter))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    parsedHeader.Parameter);
            }
            else if (httpContext is not null)
            {
                var accessToken = await httpContext.GetTokenAsync("access_token");
                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue(
                        "Bearer",
                        accessToken);
                }
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
