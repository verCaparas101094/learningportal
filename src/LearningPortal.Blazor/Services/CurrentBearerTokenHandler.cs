using System.Net.Http.Headers;

namespace LearningPortal.Blazor.Services;

/// <summary>
/// Forwards the current request's JWT bearer token to Learning Portal API requests.
/// </summary>
public sealed class CurrentBearerTokenHandler(IHttpContextAccessor httpContextAccessor)
    : DelegatingHandler
{
    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Headers.Authorization is null)
        {
            var authorizationHeader = httpContextAccessor.HttpContext?
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
        }

        return base.SendAsync(request, cancellationToken);
    }
}
