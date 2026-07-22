namespace LearningPortal.Domain.Common.Events;

/// <summary>
/// Identifies an immutable fact that occurred within the domain model.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique event identifier.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the UTC timestamp at which the event occurred.
    /// </summary>
    DateTimeOffset OccurredOnUtc { get; }
}
