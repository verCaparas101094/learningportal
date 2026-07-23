using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LearningPortal.Infrastructure.Identity;

/// <summary>
/// Provides startup integration for idempotent ASP.NET Identity role seeding.
/// </summary>
public static class IdentityRoleSeedingExtensions
{
    /// <summary>
    /// Creates missing application roles using a scoped role manager.
    /// </summary>
    /// <param name="host">The configured application host.</param>
    /// <param name="cancellationToken">Cancels the seeding operation.</param>
    public static async Task SeedIdentityRolesAsync(
        this IHost host,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(host);

        await using var scope = host.Services.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IIdentityRoleSeeder>();
        await seeder.SeedAsync(cancellationToken);
    }
}
