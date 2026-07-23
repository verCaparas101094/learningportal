using LearningPortal.Domain.Skills;

namespace LearningPortal.Domain.Repositories;

/// <summary>Provides skill persistence operations.</summary>
public interface ISkillRepository
{
    /// <summary>Gets an active skill by identifier.</summary>
    Task<Skill?> GetByIdAsync(Guid id, bool readOnly = true, CancellationToken cancellationToken = default);
    /// <summary>Gets a skill by normalized slug.</summary>
    Task<Skill?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    /// <summary>Gets a skill by its preserved display name.</summary>
    Task<Skill?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    /// <summary>Lists active skills.</summary>
    Task<IReadOnlyList<Skill>> GetActiveAsync(CancellationToken cancellationToken = default);
    /// <summary>Adds a skill.</summary>
    Task AddAsync(Skill skill, CancellationToken cancellationToken = default);
}
