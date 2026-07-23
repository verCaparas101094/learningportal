using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace LearningPortal.Blazor.Services;

/// <summary>Restores browser sessions by rotating expiring API access tokens.</summary>
public sealed class PortalCookieAuthenticationEvents(
    PortalSessionRefreshCoordinator refreshCoordinator) : CookieAuthenticationEvents
{
    /// <inheritdoc />
    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        var returnUrl = $"{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";
        context.Response.Redirect(
            AuthenticationNavigation.BuildAccessDeniedUrl(
                isAuthenticated: false,
                returnUrl));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task RedirectToAccessDenied(
        RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.Redirect("/access-denied?reason=forbidden");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var expiresText = context.Properties.GetTokenValue("access_token_expires_at");
        if (DateTimeOffset.TryParse(expiresText, out var expiresAt)
            && expiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            return;
        }

        var refreshToken = context.Properties.GetTokenValue("refresh_token");
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            context.RejectPrincipal();
            return;
        }

        var authentication = await refreshCoordinator.RefreshAsync(refreshToken);
        if (authentication is null)
        {
            context.RejectPrincipal();
            return;
        }

        context.ReplacePrincipal(
            PortalAuthenticationEndpoints.CreatePrincipal(authentication.AccessToken));
        var replacement = PortalAuthenticationEndpoints.CreateAuthenticationProperties(
            authentication,
            context.Properties.RedirectUri ?? "/",
            context.Properties.IsPersistent);
        context.Properties.ExpiresUtc = replacement.ExpiresUtc;
        context.Properties.StoreTokens(replacement.GetTokens());
        context.ShouldRenew = true;
    }
}
