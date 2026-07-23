using LearningPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LearningPortal.Infrastructure.Identity;

/// <summary>Provides explicit development-only database initialization.</summary>
public static class DevelopmentDataSeedingExtensions
{
    /// <summary>Optionally migrates and seeds the configured development database.</summary>
    public static async Task InitializeDevelopmentDataAsync(
        this IHost host,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(host);
        var environment = host.Services.GetRequiredService<IHostEnvironment>();
        if (!environment.IsDevelopment())
        {
            return;
        }

        await using var scope = host.Services.CreateAsyncScope();
        var seedOptions = scope.ServiceProvider.GetRequiredService<DevelopmentSeedOptions>();
        var databaseOptions = scope.ServiceProvider
            .GetRequiredService<DatabaseInitializationOptions>();
        if (!seedOptions.Enabled && !databaseOptions.ApplyMigrations)
        {
            return;
        }

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (databaseOptions.ApplyMigrations)
        {
            await context.Database.MigrateAsync(cancellationToken);
        }

        if (!seedOptions.Enabled)
        {
            return;
        }

        if (!await context.Database.CanConnectAsync(cancellationToken))
        {
            scope.ServiceProvider.GetRequiredService<ILogger<DevelopmentDataSeeder>>()
                .LogWarning(
                    "Development data was not seeded because the configured database is unavailable.");
            return;
        }

        await scope.ServiceProvider.GetRequiredService<IDevelopmentDataSeeder>()
            .SeedAsync(cancellationToken);
    }
}
