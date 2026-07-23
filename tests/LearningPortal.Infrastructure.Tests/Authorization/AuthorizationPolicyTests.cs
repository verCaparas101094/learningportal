using LearningPortal.Application.Authorization;
using LearningPortal.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace LearningPortal.Infrastructure.Tests.Authorization;

/// <summary>
/// Verifies registration and role composition for every named authorization policy.
/// </summary>
public sealed class AuthorizationPolicyTests
{
    /// <summary>Verifies the authenticated fallback policy and all role policies.</summary>
    [Fact]
    public async Task AddApplicationAuthorization_RegistersExpectedPolicies()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationAuthorization();
        await using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        Assert.Contains(
            options.FallbackPolicy!.Requirements,
            requirement => requirement is DenyAnonymousAuthorizationRequirement);
        AssertPolicyRoles(
            options,
            Policies.AdminOnly,
            ApplicationRoles.Administrator);
        AssertPolicyRoles(
            options,
            Policies.InstructorOnly,
            ApplicationRoles.Administrator,
            ApplicationRoles.Instructor);
        AssertPolicyRoles(
            options,
            Policies.StudentOnly,
            ApplicationRoles.Administrator,
            ApplicationRoles.Student);
        AssertPolicyRoles(
            options,
            Policies.AdminOrInstructor,
            ApplicationRoles.Administrator,
            ApplicationRoles.Instructor);
    }

    private static void AssertPolicyRoles(
        AuthorizationOptions options,
        string policyName,
        params string[] expectedRoles)
    {
        var policy = options.GetPolicy(policyName);
        var roleRequirement = Assert.Single(policy!.Requirements.OfType<RolesAuthorizationRequirement>());
        Assert.Equal(expectedRoles, roleRequirement.AllowedRoles);
    }
}
