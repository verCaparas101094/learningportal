namespace LearningPortal.Domain.Common;

/// <summary>
/// Identifies an entity whose deletion is represented as retained audit state.
/// </summary>
public interface ISoftDelete
{
    /// <summary>
    /// Gets a value indicating whether the entity has been deleted.
    /// </summary>
    bool IsDeleted { get; }

    /// <summary>
    /// Gets the UTC timestamp at which the entity was deleted.
    /// </summary>
    DateTimeOffset? DeletedAtUtc { get; }

    /// <summary>
    /// Gets the identifier of the user who deleted the entity, when available.
    /// </summary>
    Guid? DeletedBy { get; }
}
