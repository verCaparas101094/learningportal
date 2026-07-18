using LearningPortal.Blazor.Services;

namespace LearningPortal.Blazor;

/// <summary>Registers services owned by the Blazor presentation host.</summary>
public static class DependencyInjection
{
    /// <summary>Adds Razor components, the API client, and host health checks.</summary>
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
        services.AddHttpClient<LearningPortalApiClient>(client => client.BaseAddress = apiUri);
        services.AddHealthChecks();

        return services;
    }
}
