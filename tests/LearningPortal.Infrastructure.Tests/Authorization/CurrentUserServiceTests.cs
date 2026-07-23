using System.Security.Claims;
using LearningPortal.Application.Authorization;
using LearningPortal.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace LearningPortal.Infrastructure.Tests.Authorization;

/// <summary>
/// Verifies claim projection and role helpers for the current authenticated principal.
/// </summary>
public sealed class CurrentUserServiceTests
{
    /// <summary>Verifies that authenticated JWT claims are exposed without duplicates.</summary>
    [Fact]
    public void AuthenticatedPrincipal_ExposesIdentityClaimsAndRoles()
    {
        var userId = Guid.CreateVersion7();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ApplicationClaimTypes.UserId, userId.ToString()),
                new Claim(ApplicationClaimTypes.DisplayName, "Enterprise Learner"),
                new Claim(ApplicationClaimTypes.Email, "learner@example.com"),
                new Claim(ApplicationClaimTypes.Role, ApplicationRoles.Instructor),
                new Claim(ApplicationClaimTypes.Role, "instructor"),
                new Claim(ApplicationClaimTypes.Role, "UnrecognizedRole"),
                new Claim(ApplicationClaimTypes.Permission, "courses.create")
            ],
            "Bearer",
            ApplicationClaimTypes.DisplayName,
            ApplicationClaimTypes.Role));
        var httpContext = new DefaultHttpContext { User = principal };
        var service = new CurrentUserService(new HttpContextAccessor { HttpContext = httpContext });

        Assert.True(service.IsAuthenticated);
        Assert.Equal(userId, service.UserId);
        Assert.Equal("Enterprise Learner", service.DisplayName);
        Assert.Equal("learner@example.com", service.Email);
        Assert.Equal(
            [ApplicationRoles.Instructor, "UnrecognizedRole"],
            service.Roles);
        Assert.True(service.HasRole(ApplicationRoles.Instructor));
        Assert.False(service.HasRole("UnrecognizedRole"));
        Assert.True(service.HasClaim(ApplicationClaimTypes.Permission, "courses.create"));
        Assert.False(service.HasClaim(ApplicationClaimTypes.Permission, "courses.delete"));
    }

    /// <summary>Verifies that unauthenticated principals expose no current-user data.</summary>
    [Fact]
    public void UnauthenticatedPrincipal_ExposesNoIdentityData()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };
        var service = new CurrentUserService(new HttpContextAccessor { HttpContext = httpContext });

        Assert.False(service.IsAuthenticated);
        Assert.Null(service.UserId);
        Assert.Null(service.DisplayName);
        Assert.Null(service.Email);
        Assert.Empty(service.Roles);
        Assert.False(service.HasRole(ApplicationRoles.Student));
        Assert.False(service.HasClaim(ApplicationClaimTypes.Email));
    }
}
