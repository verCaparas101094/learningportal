namespace LearningPortal.Infrastructure.Identity;

/// <summary>
/// Ensures that every supported application role exists in ASP.NET Identity.
/// </summary>
public interface IIdentityRoleSeeder
{
    /// <summary>Creates missing application roles without modifying existing roles.</summary>
    /// <param name="cancellationToken">Cancels the seeding operation.</param>
    Task SeedAsync(CancellationToken cancellationToken = default);
}
