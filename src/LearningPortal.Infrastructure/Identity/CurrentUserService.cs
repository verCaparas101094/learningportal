using System.Security.Claims;
using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Authorization;
using Microsoft.AspNetCore.Http;

namespace LearningPortal.Infrastructure.Identity;

/// <summary>
/// Resolves the current authenticated user's claims from the active HTTP context.
/// </summary>
public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    /// <inheritdoc />
    public Guid? UserId
    {
        get
        {
            if (!IsAuthenticated)
            {
                return null;
            }

            var value = Principal?.FindFirst(ApplicationClaimTypes.UserId)?.Value
                ?? Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(value, out var userId) ? userId : null;
        }
    }

    /// <inheritdoc />
    public string? DisplayName =>
        IsAuthenticated
            ? Principal?.FindFirst(ApplicationClaimTypes.DisplayName)?.Value
                ?? Principal?.FindFirst(ClaimTypes.Name)?.Value
            : null;

    /// <inheritdoc />
    public string? Email =>
        IsAuthenticated
            ? Principal?.FindFirst(ApplicationClaimTypes.Email)?.Value
                ?? Principal?.FindFirst(ClaimTypes.Email)?.Value
            : null;

    /// <inheritdoc />
    public IReadOnlyCollection<string> Roles =>
        IsAuthenticated
            ? Principal?.Claims
                .Where(claim => claim.Type is ApplicationClaimTypes.Role or ClaimTypes.Role)
                .Select(claim => claim.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? []
            : [];

    /// <inheritdoc />
    public bool IsAuthenticated =>
        Principal?.Identity?.IsAuthenticated == true;

    /// <inheritdoc />
    public bool HasRole(string role) =>
        ApplicationRoles.IsValid(role)
        && Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public bool HasClaim(string claimType, string? claimValue = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(claimType);

        return IsAuthenticated
            && Principal?.Claims.Any(claim =>
                string.Equals(claim.Type, claimType, StringComparison.Ordinal)
                && (claimValue is null
                    || string.Equals(claim.Value, claimValue, StringComparison.Ordinal))) == true;
    }

    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;
}
