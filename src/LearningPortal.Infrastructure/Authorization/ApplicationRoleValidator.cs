using LearningPortal.Application.Authorization;
using Microsoft.AspNetCore.Identity;

namespace LearningPortal.Infrastructure.Authorization;

/// <summary>
/// Prevents ASP.NET Identity from creating roles outside the application role allowlist.
/// </summary>
public sealed class ApplicationRoleValidator : IRoleValidator<IdentityRole<Guid>>
{
    /// <inheritdoc />
    public Task<IdentityResult> ValidateAsync(
        RoleManager<IdentityRole<Guid>> manager,
        IdentityRole<Guid> role)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(role);

        return Task.FromResult(
            ApplicationRoles.IsValid(role.Name)
                ? IdentityResult.Success
                : IdentityResult.Failed(new IdentityError
                {
                    Code = "Role.Invalid",
                    Description = "The role is not recognized by the application."
                }));
    }
}
