namespace LearningPortal.Application.Abstractions.Time;

/// <summary>
/// Provides the current time through an abstraction suitable for deterministic testing.
/// </summary>
public interface ISystemClock
{
    /// <summary>
    /// Gets the current UTC date and time.
    /// </summary>
    DateTimeOffset UtcNow { get; }
}
