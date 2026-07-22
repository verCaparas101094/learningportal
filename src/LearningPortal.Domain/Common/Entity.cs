using LearningPortal.Domain.Common.Events;

namespace LearningPortal.Domain.Common;

/// <summary>
/// Provides strongly typed identity for persisted domain entities.
/// </summary>
public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Gets the entity identifier.
    /// </summary>
    public Guid Id { get; protected set; } = Guid.NewGuid();

    /// <summary>
    /// Gets a read-only view of domain events raised by this entity.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adds a domain event raised by this entity.
    /// </summary>
    /// <param name="domainEvent">The domain event to add.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="domainEvent"/> is <see langword="null"/>.
    /// </exception>
    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Removes a domain event from this entity when it is present.
    /// </summary>
    /// <param name="domainEvent">The domain event to remove.</param>
    /// <returns><see langword="true"/> when the event was removed; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="domainEvent"/> is <see langword="null"/>.
    /// </exception>
    public bool RemoveDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        return _domainEvents.Remove(domainEvent);
    }

    /// <summary>
    /// Removes all accumulated domain events from this entity.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
