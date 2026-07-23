using LearningPortal.Infrastructure.Identity;

namespace LearningPortal.Infrastructure.IntegrationTests.Authentication;

/// <summary>
/// Adds a deterministic concurrency barrier before delegating to the production JWT generator.
/// </summary>
internal sealed class CoordinatedAccessTokenGenerator(
    JwtAccessTokenGenerator inner,
    RefreshRotationCoordinator coordinator)
    : IAccessTokenGenerator
{
    /// <inheritdoc />
    public async Task<GeneratedAccessToken> GenerateAsync(
        ApplicationUser user,
        CancellationToken cancellationToken = default)
    {
        await coordinator.WaitAsync(cancellationToken);
        return await inner.GenerateAsync(user, cancellationToken);
    }
}
