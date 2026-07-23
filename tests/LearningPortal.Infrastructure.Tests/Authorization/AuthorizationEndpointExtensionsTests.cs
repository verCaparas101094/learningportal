using LearningPortal.Api.Extensions;
using LearningPortal.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace LearningPortal.Infrastructure.Tests.Authorization;

/// <summary>
/// Verifies the strongly named Minimal API authorization conventions.
/// </summary>
public sealed class AuthorizationEndpointExtensionsTests
{
    /// <summary>Verifies that each helper adds the expected named policy metadata.</summary>
    [Fact]
    public async Task AuthorizationHelpers_AddExpectedPolicies()
    {
        var builder = WebApplication.CreateBuilder();
        await using var app = builder.Build();
        app.MapGet("/admin", () => Results.Ok()).RequireAdministrator();
        app.MapGet("/instructor", () => Results.Ok()).RequireInstructor();
        app.MapGet("/student", () => Results.Ok()).RequireStudent();
        app.MapGet("/admin-or-instructor", () => Results.Ok()).RequireAdminOrInstructor();

        AssertEndpointPolicy(app, "/admin", Policies.AdminOnly);
        AssertEndpointPolicy(app, "/instructor", Policies.InstructorOnly);
        AssertEndpointPolicy(app, "/student", Policies.StudentOnly);
        AssertEndpointPolicy(app, "/admin-or-instructor", Policies.AdminOrInstructor);
    }

    private static void AssertEndpointPolicy(
        WebApplication app,
        string route,
        string expectedPolicy)
    {
        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(candidate => candidate.RoutePattern.RawText == route);
        var authorization = Assert.Single(endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>());

        Assert.Equal(expectedPolicy, authorization.Policy);
    }
}
