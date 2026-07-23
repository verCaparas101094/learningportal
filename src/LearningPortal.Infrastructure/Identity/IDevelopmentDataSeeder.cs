namespace LearningPortal.Infrastructure.Identity;

/// <summary>Seeds local demonstration users and learning data idempotently.</summary>
public interface IDevelopmentDataSeeder
{
    /// <summary>Seeds missing development data.</summary>
    Task SeedAsync(CancellationToken cancellationToken = default);
}
