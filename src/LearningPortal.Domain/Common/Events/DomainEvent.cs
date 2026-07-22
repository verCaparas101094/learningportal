namespace LearningPortal.Domain.Common.Events;

/// <summary>
/// Provides immutable identity and occurrence metadata for a domain event.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    /// <summary>
    /// Initializes a new domain event with a time-ordered identifier and current UTC timestamp.
    /// </summary>
    protected DomainEvent()
    {
        EventId = Guid.CreateVersion7();
        OccurredOnUtc = TimeProvider.System.GetUtcNow();
    }

    /// <inheritdoc />
    public Guid EventId { get; }

    /// <inheritdoc />
    public DateTimeOffset OccurredOnUtc { get; }
}
