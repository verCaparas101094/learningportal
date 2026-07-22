using LearningPortal.Application.Abstractions.Time;

namespace LearningPortal.Infrastructure.Time;

/// <summary>
/// Provides UTC time from the platform <see cref="TimeProvider"/>.
/// </summary>
public sealed class SystemClock(TimeProvider timeProvider) : ISystemClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();
}
