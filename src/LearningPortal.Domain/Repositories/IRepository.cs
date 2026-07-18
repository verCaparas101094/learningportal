using LearningPortal.Domain.Common;

namespace LearningPortal.Domain.Repositories;

/// <summary>Defines asynchronous persistence operations for an aggregate.</summary>
/// <typeparam name="TEntity">The aggregate entity type.</typeparam>
public interface IRepository<TEntity>
    where TEntity : Entity
{
    /// <summary>Returns all entities without tracking.</summary>
    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds an entity to the current unit of work.</summary>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
}
