using LearningPortal.Api.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace LearningPortal.Api.Endpoints;

/// <summary>Maps liveness and readiness probes.</summary>
public static class HealthEndpoints
{
    /// <summary>Maps health endpoints for orchestrators and load balancers.</summary>
    public static IEndpointRouteBuilder MapPortalHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = HealthResponseWriter.WriteAsync
        }).AllowAnonymous();

        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("ready"),
            ResponseWriter = HealthResponseWriter.WriteAsync
        }).AllowAnonymous();

        return endpoints;
    }
}
