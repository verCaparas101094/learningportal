using LearningPortal.Application.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace LearningPortal.Infrastructure.Identity;

/// <summary>
/// Idempotently seeds the fixed LearningPortal role allowlist into ASP.NET Identity.
/// </summary>
public sealed class IdentityRoleSeeder(
    RoleManager<IdentityRole<Guid>> roleManager,
    ILogger<IdentityRoleSeeder> logger)
    : IIdentityRoleSeeder
{
    /// <inheritdoc />
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var roleName in ApplicationRoles.All)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole<Guid>
            {
                Id = Guid.CreateVersion7(),
                Name = roleName
            });

            if (result.Succeeded)
            {
                logger.LogInformation("Created application role {RoleName}.", roleName);
                continue;
            }

            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            throw new InvalidOperationException(
                $"Unable to create application role '{roleName}': "
                + string.Join("; ", result.Errors.Select(error => error.Code)));
        }
    }
}
