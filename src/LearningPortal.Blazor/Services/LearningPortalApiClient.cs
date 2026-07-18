using System.Net.Http.Json;
using LearningPortal.Blazor.Models;

namespace LearningPortal.Blazor.Services;

/// <summary>Provides typed, asynchronous access to the Learning Portal API.</summary>
public sealed class LearningPortalApiClient(HttpClient httpClient)
{
    /// <summary>Gets the API liveness state.</summary>
    public async Task<ApiHealthResponse> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetFromJsonAsync<ApiHealthResponse>("health/live", cancellationToken);
        return response ?? throw new InvalidOperationException("The API returned an empty health response.");
    }
}
