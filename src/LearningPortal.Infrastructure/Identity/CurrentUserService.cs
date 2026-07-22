using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LearningPortal.Application.Abstractions.Identity;
using Microsoft.AspNetCore.Http;

namespace LearningPortal.Infrastructure.Identity;

/// <summary>
/// Resolves the current authenticated user's identifier from the active HTTP context.
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

            var principal = httpContextAccessor.HttpContext?.User;
            var value = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return Guid.TryParse(value, out var userId) ? userId : null;
        }
    }

    /// <inheritdoc />
    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;
}
