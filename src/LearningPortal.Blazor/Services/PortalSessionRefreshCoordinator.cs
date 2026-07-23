using System.Collections.Concurrent;
using System.Net.Http.Json;
using LearningPortal.Shared.Identity;

namespace LearningPortal.Blazor.Services;

/// <summary>Coalesces concurrent browser refresh-token rotations.</summary>
public sealed class PortalSessionRefreshCoordinator(IHttpClientFactory httpClientFactory)
{
    private readonly ConcurrentDictionary<string, Lazy<Task<AuthenticationResponse?>>> _refreshes =
        new(StringComparer.Ordinal);

    /// <summary>Refreshes one browser session without rotating the same token concurrently.</summary>
    public async Task<AuthenticationResponse?> RefreshAsync(string refreshToken)
    {
        var operation = _refreshes.GetOrAdd(
            refreshToken,
            token => new Lazy<Task<AuthenticationResponse?>>(
                () => RefreshCoreAsync(token),
                LazyThreadSafetyMode.ExecutionAndPublication));
        try
        {
            return await operation.Value;
        }
        finally
        {
            _ = RemoveLaterAsync(refreshToken, operation);
        }
    }

    private async Task<AuthenticationResponse?> RefreshCoreAsync(string refreshToken)
    {
        try
        {
            var client = httpClientFactory.CreateClient(
                PortalAuthenticationEndpoints.ApiClientName);
            using var response = await client.PostAsJsonAsync(
                "api/auth/refresh",
                new RefreshTokenRequest(refreshToken));
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<AuthenticationResponse>()
                : null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private async Task RemoveLaterAsync(
        string refreshToken,
        Lazy<Task<AuthenticationResponse?>> operation)
    {
        await Task.Delay(TimeSpan.FromSeconds(10));
        _refreshes.TryRemove(
            new KeyValuePair<string, Lazy<Task<AuthenticationResponse?>>>(
                refreshToken,
                operation));
    }
}
