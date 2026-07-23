using LearningPortal.Application.Authorization;

namespace LearningPortal.Api.Extensions;

/// <summary>
/// Provides strongly named authorization conventions for Minimal API endpoints and groups.
/// </summary>
public static class AuthorizationEndpointExtensions
{
    /// <summary>Requires the Administrator policy.</summary>
    public static TBuilder RequireAdministrator<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireAuthorization(Policies.AdminOnly);

    /// <summary>Requires the Instructor policy, which also permits administrators.</summary>
    public static TBuilder RequireInstructor<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireAuthorization(Policies.InstructorOnly);

    /// <summary>Requires the Student policy, which also permits administrators.</summary>
    public static TBuilder RequireStudent<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireAuthorization(Policies.StudentOnly);

    /// <summary>Requires either the Administrator or Instructor policy.</summary>
    public static TBuilder RequireAdminOrInstructor<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireAuthorization(Policies.AdminOrInstructor);
}
