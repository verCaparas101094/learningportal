using LearningPortal.Blazor.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace LearningPortal.Blazor;

/// <summary>Registers services owned by the Blazor presentation host.</summary>
public static class DependencyInjection
{
    /// <summary>Adds Razor components, component authorization, the API client, and host health checks.</summary>
    public static IServiceCollection AddBlazorPresentation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var apiBaseUrl = configuration["Api:BaseUrl"];
        if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiUri))
        {
            throw new InvalidOperationException("A valid absolute Api:BaseUrl must be configured.");
        }

        services.AddRazorComponents().AddInteractiveServerComponents();
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = AuthenticationNavigation.SignInRoute;
                options.AccessDeniedPath = "/access-denied?reason=forbidden";
                options.Cookie.Name = "__Host-LearningPortal.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.SlidingExpiration = false;
                options.EventsType = typeof(PortalCookieAuthenticationEvents);
            });
        services.AddAuthorizationCore();
        services.AddCascadingAuthenticationState();
        services.AddHttpContextAccessor();
        services.AddScoped<PortalCookieAuthenticationEvents>();
        services.AddSingleton<PortalSessionRefreshCoordinator>();
        services.AddTransient<CurrentBearerTokenHandler>();
        services
            .AddHttpClient<LearningPortalApiClient>(client => client.BaseAddress = apiUri)
            .AddHttpMessageHandler<CurrentBearerTokenHandler>();
        services.AddHttpClient(
            PortalAuthenticationEndpoints.ApiClientName,
            client => client.BaseAddress = apiUri);
        services.AddHealthChecks();

        return services;
    }
}
