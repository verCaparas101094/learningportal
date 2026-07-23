using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Authorization;
using LearningPortal.Infrastructure.Authorization;
using LearningPortal.Infrastructure.Identity;
using LearningPortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LearningPortal.Infrastructure.Tests.UserManagement;

/// <summary>
/// Verifies the essential Identity-backed administrator user-management behavior.
/// </summary>
public sealed class UserManagementServiceTests
{
    /// <summary>Verifies filtered pagination and the maximum page-size rule.</summary>
    [Fact]
    public async Task GetUsersAsync_WithSearch_ReturnsRequestedPage()
    {
        await using var context = await UserManagementTestContext.CreateAsync();
        await context.CreateUserAsync("alice@example.com", "Alpha Learner");
        await context.CreateUserAsync("bob@example.com", "Alpha Instructor");
        await context.CreateUserAsync("charlie@example.com", "Other User");
        await context.CreateUserAsync("delta@example.com", "Alpha Administrator");

        var result = await context.Service.GetUsersAsync("Alpha", 2, 2);
        var oversized = await context.Service.GetUsersAsync(null, 1, 101);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.TotalCount);
        Assert.Equal(2, result.Value.TotalPages);
        Assert.Equal(2, result.Value.PageNumber);
        Assert.Equal(2, result.Value.PageSize);
        Assert.Equal("delta@example.com", Assert.Single(result.Value.Items).Email);
        Assert.True(oversized.IsFailure);
        Assert.Equal("Validation.Failed", oversized.Error?.Code);
    }

    /// <summary>Verifies the stable not-found Result for an unknown user.</summary>
    [Fact]
    public async Task GetUserByIdAsync_WithUnknownUser_ReturnsNotFound()
    {
        await using var context = await UserManagementTestContext.CreateAsync();
        var result = await context.Service.GetUserByIdAsync(Guid.CreateVersion7());

        Assert.True(result.IsFailure);
        Assert.Equal("UserManagement.UserNotFound", result.Error?.Code);
    }

    /// <summary>Verifies that a user can be disabled and enabled again.</summary>
    [Fact]
    public async Task SetEnabledAsync_ChangesEnabledState()
    {
        await using var context = await UserManagementTestContext.CreateAsync();
        var user = await context.CreateUserAsync("state@example.com", "State User");

        var disabled = await context.Service.SetEnabledAsync(user.Id, false);
        var enabled = await context.Service.SetEnabledAsync(user.Id, true);

        Assert.True(disabled.IsSuccess);
        Assert.False(disabled.Value.IsEnabled);
        Assert.True(enabled.IsSuccess);
        Assert.True(enabled.Value.IsEnabled);
    }

    /// <summary>Verifies additive assignment of one valid application role.</summary>
    [Fact]
    public async Task AssignRoleAsync_WithValidRole_AddsRole()
    {
        await using var context = await UserManagementTestContext.CreateAsync();
        var user = await context.CreateUserAsync("instructor@example.com", "Instructor User");

        var result = await context.Service.AssignRoleAsync(user.Id, ApplicationRoles.Instructor);

        Assert.True(result.IsSuccess);
        Assert.Contains(ApplicationRoles.Instructor, result.Value.Roles);
    }

    /// <summary>Verifies that roles outside the application allowlist are rejected.</summary>
    [Fact]
    public async Task AssignRoleAsync_WithInvalidRole_ReturnsValidationFailure()
    {
        await using var context = await UserManagementTestContext.CreateAsync();
        var user = await context.CreateUserAsync("invalid-role@example.com", "Invalid Role User");

        var result = await context.Service.AssignRoleAsync(user.Id, "Manager");

        Assert.True(result.IsFailure);
        Assert.Equal("UserManagement.InvalidRole", result.Error?.Code);
    }

    /// <summary>Verifies that repeated assignment does not create duplicate user roles.</summary>
    [Fact]
    public async Task AssignRoleAsync_WithExistingRole_RemainsIdempotent()
    {
        await using var context = await UserManagementTestContext.CreateAsync();
        var user = await context.CreateUserAsync(
            "student@example.com",
            "Student User",
            ApplicationRoles.Student);

        var first = await context.Service.AssignRoleAsync(user.Id, ApplicationRoles.Student);
        var second = await context.Service.AssignRoleAsync(user.Id, ApplicationRoles.Student);
        var roles = await context.UserManager.GetRolesAsync(user);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(1, roles.Count(role =>
            string.Equals(role, ApplicationRoles.Student, StringComparison.OrdinalIgnoreCase)));
    }
}

internal sealed class UserManagementTestContext : IAsyncDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly AsyncServiceScope _scope;

    private UserManagementTestContext(ServiceProvider serviceProvider, AsyncServiceScope scope)
    {
        _serviceProvider = serviceProvider;
        _scope = scope;
    }

    internal IUserManagementService Service =>
        _scope.ServiceProvider.GetRequiredService<IUserManagementService>();

    internal UserManager<ApplicationUser> UserManager =>
        _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    internal static async Task<UserManagementTestContext> CreateAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"UserManagementTests-{Guid.CreateVersion7()}"));
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddRoleValidator<ApplicationRoleValidator>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddScoped<IIdentityRoleSeeder, IdentityRoleSeeder>();
        services.AddScoped<IUserManagementService, UserManagementService>();

        var serviceProvider = services.BuildServiceProvider(validateScopes: true);
        var scope = serviceProvider.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IIdentityRoleSeeder>();
        await seeder.SeedAsync();

        return new UserManagementTestContext(serviceProvider, scope);
    }

    internal async Task<ApplicationUser> CreateUserAsync(
        string email,
        string displayName,
        params string[] roles)
    {
        var user = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName,
            IsEnabled = true
        };
        EnsureSucceeded(await UserManager.CreateAsync(user));

        foreach (var role in roles)
        {
            EnsureSucceeded(await UserManager.AddToRoleAsync(user, role));
        }

        return user;
    }

    public async ValueTask DisposeAsync()
    {
        await _scope.DisposeAsync();
        await _serviceProvider.DisposeAsync();
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ", result.Errors.Select(error => error.Description)));
        }
    }
}
