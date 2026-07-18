namespace LearningPortal.Domain.Common;

/// <summary>Provides identity and audit state for persisted domain entities.</summary>
public abstract class Entity
{
    /// <summary>Gets the entity identifier.</summary>
    public Guid Id { get; protected init; } = Guid.NewGuid();

    /// <summary>Gets the UTC timestamp at which the entity was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; protected init; } = DateTimeOffset.UtcNow;

    /// <summary>Gets the UTC timestamp of the most recent update.</summary>
    public DateTimeOffset? UpdatedAtUtc { get; protected set; }
}
