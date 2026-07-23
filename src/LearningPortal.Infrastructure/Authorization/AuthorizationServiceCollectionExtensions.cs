using LearningPortal.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace LearningPortal.Infrastructure.Authorization;

/// <summary>
/// Registers LearningPortal's role-based authorization policies.
/// </summary>
public static class AuthorizationServiceCollectionExtensions
{
    /// <summary>
    /// Adds the authenticated fallback policy and every named application policy.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddApplicationAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            options.AddPolicy(
                Policies.AdminOnly,
                policy => policy.RequireRole(ApplicationRoles.Administrator));
            options.AddPolicy(
                Policies.InstructorOnly,
                policy => policy.RequireRole(
                    ApplicationRoles.Administrator,
                    ApplicationRoles.Instructor));
            options.AddPolicy(
                Policies.StudentOnly,
                policy => policy.RequireRole(
                    ApplicationRoles.Administrator,
                    ApplicationRoles.Student));
            options.AddPolicy(
                Policies.AdminOrInstructor,
                policy => policy.RequireRole(
                    ApplicationRoles.Administrator,
                    ApplicationRoles.Instructor));
        });

        return services;
    }
}
