namespace LearningPortal.Domain.Repositories;

/// <summary>Coordinates atomic persistence of aggregate changes.</summary>
public interface IUnitOfWork
{
    /// <summary>Persists all pending changes.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
