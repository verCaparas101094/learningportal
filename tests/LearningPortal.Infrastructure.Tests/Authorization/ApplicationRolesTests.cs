using LearningPortal.Application.Authorization;
using Xunit;

namespace LearningPortal.Infrastructure.Tests.Authorization;

/// <summary>
/// Verifies the immutable application role allowlist.
/// </summary>
public sealed class ApplicationRolesTests
{
    /// <summary>Verifies that only the three supported role names are accepted.</summary>
    [Fact]
    public void IsValid_AcceptsOnlyApplicationRoles()
    {
        Assert.Equal(
            [ApplicationRoles.Administrator, ApplicationRoles.Instructor, ApplicationRoles.Student],
            ApplicationRoles.All);
        Assert.True(ApplicationRoles.IsValid(ApplicationRoles.Administrator));
        Assert.True(ApplicationRoles.IsValid("instructor"));
        Assert.True(ApplicationRoles.IsValid(ApplicationRoles.Student));
        Assert.False(ApplicationRoles.IsValid("Manager"));
        Assert.False(ApplicationRoles.IsValid(null));
    }
}
