namespace LearningPortal.Domain.Common;

/// <summary>
/// Provides audit metadata and optimistic concurrency state for a domain entity.
/// </summary>
public abstract class AuditableEntity : Entity
{
    /// <summary>
    /// Gets the UTC timestamp at which the entity was created.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; protected set; }

    /// <summary>
    /// Gets the identifier of the user who created the entity, when available.
    /// </summary>
    public Guid? CreatedBy { get; protected set; }

    /// <summary>
    /// Gets the UTC timestamp of the most recent update.
    /// </summary>
    public DateTimeOffset? UpdatedAtUtc { get; protected set; }

    /// <summary>
    /// Gets the identifier of the user who most recently updated the entity, when available.
    /// </summary>
    public Guid? UpdatedBy { get; protected set; }

    /// <summary>
    /// Gets the SQL Server rowversion used for optimistic concurrency checks.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];
}
