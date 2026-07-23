using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using LearningPortal.Application.Authorization;
using LearningPortal.Shared.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.WebUtilities;

namespace LearningPortal.Blazor.Services;

/// <summary>Maps the browser-facing cookie bridge to the existing authentication API.</summary>
public static class PortalAuthenticationEndpoints
{
    /// <summary>Gets the named client used only for anonymous authentication calls.</summary>
    public const string ApiClientName = "LearningPortal.AuthenticationApi";

    /// <summary>Maps browser sign-in using the existing API login operation.</summary>
    public static IEndpointRouteBuilder MapPortalAuthenticationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/auth/login", LoginAsync).AllowAnonymous();
        endpoints.MapPost("/auth/register", RegisterAsync).AllowAnonymous();
        endpoints.MapPost("/auth/logout", LogoutAsync).RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        HttpContext context,
        IHttpClientFactory httpClientFactory,
        IAntiforgery antiforgery,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(context);
        var form = await context.Request.ReadFormAsync(cancellationToken);
        var email = form["email"].ToString().Trim();
        var password = form["password"].ToString();
        var returnUrl = AuthenticationNavigation.NormalizeLocalReturnUrl(
            form["returnUrl"].ToString());
        var rememberMe = string.Equals(
            form["rememberMe"].ToString(), "true", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return LoginFailure(returnUrl, "required");
        }

        var client = httpClientFactory.CreateClient(ApiClientName);
        using var response = await client.PostAsJsonAsync(
            "api/auth/login",
            new LoginRequest(email, password),
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return LoginFailure(returnUrl, "invalid");
        }

        var authentication = await response.Content
            .ReadFromJsonAsync<AuthenticationResponse>(cancellationToken);
        if (authentication is null)
        {
            return LoginFailure(returnUrl, "unavailable");
        }

        var principal = CreatePrincipal(authentication.AccessToken);
        var properties = CreateAuthenticationProperties(
            authentication, returnUrl, rememberMe);

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            properties);
        return Results.LocalRedirect(returnUrl);
    }

    private static async Task<IResult> RegisterAsync(
        HttpContext context,
        IHttpClientFactory httpClientFactory,
        IAntiforgery antiforgery,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(context);
        var form = await context.Request.ReadFormAsync(cancellationToken);
        var returnUrl = AuthenticationNavigation.NormalizeLocalReturnUrl(
            form["returnUrl"].ToString());
        var request = new RegisterRequest(
            form["firstName"].ToString(),
            form["lastName"].ToString(),
            form["email"].ToString(),
            form["password"].ToString(),
            form["confirmPassword"].ToString());
        var client = httpClientFactory.CreateClient(ApiClientName);
        using var response = await client.PostAsJsonAsync(
            "api/auth/register", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return Results.LocalRedirect(QueryHelpers.AddQueryString(
                "/register",
                new Dictionary<string, string?>
                {
                    ["error"] = "invalid",
                    ["returnUrl"] = returnUrl
                }));
        }

        var authentication = await response.Content
            .ReadFromJsonAsync<AuthenticationResponse>(cancellationToken);
        if (authentication is null)
        {
            return Results.LocalRedirect("/register?error=unavailable");
        }

        var properties = CreateAuthenticationProperties(
            authentication, returnUrl, isPersistent: false);
        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            CreatePrincipal(authentication.AccessToken),
            properties);
        return Results.LocalRedirect(returnUrl);
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext context,
        IHttpClientFactory httpClientFactory,
        IAntiforgery antiforgery,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(context);
        var refreshToken = await context.GetTokenAsync("refresh_token");
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            try
            {
                var client = httpClientFactory.CreateClient(ApiClientName);
                using var response = await client.PostAsJsonAsync(
                    "api/auth/revoke",
                    new RevokeTokenRequest(refreshToken),
                    cancellationToken);
            }
            catch (HttpRequestException)
            {
                // Local sign-out must still succeed when the API is unavailable.
            }
        }

        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.LocalRedirect("/login?signedOut=true");
    }

    private static IResult LoginFailure(string returnUrl, string error)
    {
        var query = new Dictionary<string, string?>
        {
            ["error"] = error,
            ["returnUrl"] = returnUrl
        };
        return Results.LocalRedirect(QueryHelpers.AddQueryString("/login", query));
    }

    internal static ClaimsPrincipal CreatePrincipal(string accessToken)
    {
        var identity = new ClaimsIdentity(
            ReadClaims(accessToken),
            CookieAuthenticationDefaults.AuthenticationScheme,
            ApplicationClaimTypes.DisplayName,
            ApplicationClaimTypes.Role);
        return new ClaimsPrincipal(identity);
    }

    internal static AuthenticationProperties CreateAuthenticationProperties(
        AuthenticationResponse authentication,
        string returnUrl,
        bool isPersistent)
    {
        var properties = new AuthenticationProperties
        {
            IsPersistent = isPersistent,
            AllowRefresh = true,
            ExpiresUtc = authentication.RefreshTokenExpiresAtUtc,
            RedirectUri = returnUrl
        };
        properties.StoreTokens(
        [
            new AuthenticationToken { Name = "access_token", Value = authentication.AccessToken },
            new AuthenticationToken { Name = "refresh_token", Value = authentication.RefreshToken },
            new AuthenticationToken
            {
                Name = "access_token_expires_at",
                Value = authentication.AccessTokenExpiresAtUtc.ToString("O")
            },
            new AuthenticationToken
            {
                Name = "refresh_token_expires_at",
                Value = authentication.RefreshTokenExpiresAtUtc.ToString("O")
            }
        ]);
        return properties;
    }

    private static IReadOnlyCollection<Claim> ReadClaims(string accessToken)
    {
        var segments = accessToken.Split('.');
        if (segments.Length != 3)
        {
            throw new InvalidOperationException("The authentication API returned an invalid access token.");
        }

        using var payload = JsonDocument.Parse(WebEncoders.Base64UrlDecode(segments[1]));
        var claims = new List<Claim>();
        foreach (var property in payload.RootElement.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Array)
            {
                claims.AddRange(property.Value.EnumerateArray().Select(
                    value => new Claim(property.Name, ClaimValue(value))));
            }
            else
            {
                claims.Add(new Claim(property.Name, ClaimValue(property.Value)));
            }
        }

        return claims;
    }

    private static string ClaimValue(JsonElement value) =>
        value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : value.GetRawText();
}
