using LearningPortal.Application.Authorization;
using LearningPortal.Infrastructure.Authorization;
using LearningPortal.Infrastructure.Identity;
using LearningPortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LearningPortal.Infrastructure.Tests.Authorization;

/// <summary>
/// Verifies idempotent role seeding and Identity role-name validation.
/// </summary>
public sealed class IdentityRoleSeederTests
{
    /// <summary>Verifies that repeated seeding creates each supported role exactly once.</summary>
    [Fact]
    public async Task SeedAsync_WhenRepeated_CreatesOnlyMissingRoles()
    {
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IIdentityRoleSeeder>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        var roles = await roleManager.Roles
            .Select(role => role.Name!)
            .OrderBy(role => role)
            .ToListAsync();
        Assert.Equal(ApplicationRoles.All.OrderBy(role => role), roles);
    }

    /// <summary>Verifies that Identity rejects role creation and assignment outside the allowlist.</summary>
    [Fact]
    public async Task Identity_WithUnknownRole_RejectsCreationAndAssignment()
    {
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var result = await roleManager.CreateAsync(new IdentityRole<Guid>
        {
            Id = Guid.CreateVersion7(),
            Name = "Manager"
        });

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == "Role.Invalid");

        var user = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            UserName = "role-validation@example.com",
            Email = "role-validation@example.com",
            EmailConfirmed = true
        };
        Assert.True((await userManager.CreateAsync(user)).Succeeded);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => userManager.AddToRoleAsync(user, "Manager"));
    }

    /// <summary>Verifies configured bootstrap administration is idempotent.</summary>
    [Fact]
    public async Task SeedAsync_WithBootstrapAdministrator_CreatesEnabledAdministratorOnce()
    {
        await using var provider = CreateServiceProvider(new BootstrapAdministratorOptions
        {
            Enabled = true,
            Email = "bootstrap@example.com",
            Password = "Strong-Bootstrap-Password-123!",
            DisplayName = "Bootstrap Administrator"
        });
        await using var scope = provider.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IIdentityRoleSeeder>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        var user = await userManager.FindByEmailAsync("bootstrap@example.com");
        Assert.NotNull(user);
        Assert.True(user.EmailConfirmed);
        Assert.True(user.IsEnabled);
        Assert.Equal("Bootstrap Administrator", user.DisplayName);
        Assert.True(await userManager.IsInRoleAsync(user, ApplicationRoles.Administrator));
        Assert.Single(userManager.Users);
    }

    private static ServiceProvider CreateServiceProvider(
        BootstrapAdministratorOptions? bootstrapAdministrator = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"RoleSeederTests-{Guid.CreateVersion7()}"));
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddRoleValidator<ApplicationRoleValidator>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddScoped<IIdentityRoleSeeder, IdentityRoleSeeder>();
        services.AddSingleton(
            bootstrapAdministrator ?? new BootstrapAdministratorOptions());

        return services.BuildServiceProvider(validateScopes: true);
    }
}
