using LearningPortal.Domain.Common;
using LearningPortal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LearningPortal.Infrastructure.Persistence.Repositories;

/// <summary>Implements aggregate persistence with EF Core.</summary>
public sealed class Repository<TEntity>(ApplicationDbContext dbContext) : IRepository<TEntity>
    where TEntity : Entity
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Set<TEntity>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await dbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
}
