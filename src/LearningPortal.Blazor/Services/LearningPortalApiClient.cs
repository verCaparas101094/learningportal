using System.Net.Http.Json;
using LearningPortal.Blazor.Models;
using LearningPortal.Shared.UserManagement;

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

    /// <summary>Gets one filtered, paginated page of administrator-safe users.</summary>
    /// <param name="request">The search and pagination values.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The matching page of users.</returns>
    public async Task<PagedUsersResponse> GetUsersAsync(
        GetUsersRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestUri = $"api/users?PageNumber={request.PageNumber}&PageSize={request.PageSize}";
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            requestUri += $"&Search={Uri.EscapeDataString(request.Search.Trim())}";
        }

        var response = await httpClient.GetFromJsonAsync<PagedUsersResponse>(
            requestUri,
            cancellationToken);

        return response ?? throw new InvalidOperationException("The API returned an empty users response.");
    }

    /// <summary>Enables one user account.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The updated user.</returns>
    public Task<UserResponse> EnableUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        PutAsync<UserResponse>($"api/users/{userId:D}/enable", null, cancellationToken);

    /// <summary>Disables one user account.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The updated user.</returns>
    public Task<UserResponse> DisableUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        PutAsync<UserResponse>($"api/users/{userId:D}/disable", null, cancellationToken);

    /// <summary>Adds one valid application role to a user.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="request">The role assignment.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The updated user.</returns>
    public Task<UserResponse> AssignUserRoleAsync(
        Guid userId,
        AssignUserRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PutAsync<UserResponse>($"api/users/{userId:D}/roles", request, cancellationToken);
    }

    private async Task<TResponse> PutAsync<TResponse>(
        string requestUri,
        object? request,
        CancellationToken cancellationToken)
    {
        using var response = request is null
            ? await httpClient.PutAsync(requestUri, content: null, cancellationToken)
            : await httpClient.PutAsJsonAsync(requestUri, request, cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken);
        return result ?? throw new InvalidOperationException("The API returned an empty response.");
    }
}
