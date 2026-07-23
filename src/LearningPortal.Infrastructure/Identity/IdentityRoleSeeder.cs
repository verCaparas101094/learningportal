using LearningPortal.Application.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace LearningPortal.Infrastructure.Identity;

/// <summary>
/// Idempotently seeds the fixed LearningPortal role allowlist into ASP.NET Identity.
/// </summary>
public sealed class IdentityRoleSeeder(
    RoleManager<IdentityRole<Guid>> roleManager,
    UserManager<ApplicationUser> userManager,
    BootstrapAdministratorOptions bootstrapAdministrator,
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

        await SeedBootstrapAdministratorAsync(cancellationToken);
    }

    private async Task SeedBootstrapAdministratorAsync(
        CancellationToken cancellationToken)
    {
        if (!bootstrapAdministrator.Enabled)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var email = bootstrapAdministrator.Email.Trim();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.CreateVersion7(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = bootstrapAdministrator.DisplayName.Trim(),
                IsEnabled = true
            };
            var createResult = await userManager.CreateAsync(
                user,
                bootstrapAdministrator.Password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Unable to create the bootstrap administrator: "
                    + string.Join("; ", createResult.Errors.Select(error => error.Code)));
            }

            logger.LogInformation("Created the configured bootstrap administrator account.");
        }

        if (!user.IsEnabled || !user.EmailConfirmed)
        {
            user.IsEnabled = true;
            user.EmailConfirmed = true;
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Unable to enable the bootstrap administrator: "
                    + string.Join("; ", updateResult.Errors.Select(error => error.Code)));
            }
        }

        if (!await userManager.IsInRoleAsync(user, ApplicationRoles.Administrator))
        {
            var roleResult = await userManager.AddToRoleAsync(
                user,
                ApplicationRoles.Administrator);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Unable to assign the bootstrap administrator role: "
                    + string.Join("; ", roleResult.Errors.Select(error => error.Code)));
            }
        }
    }
}
