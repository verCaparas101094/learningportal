namespace LearningPortal.Domain.Common;

/// <summary>
/// Provides strongly typed identity for persisted domain entities.
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// Gets the entity identifier.
    /// </summary>
    public Guid Id { get; protected set; } = Guid.NewGuid();
}
