using LearningPortal.Domain.Quizzes;
using LearningPortal.Domain.Skills;

namespace LearningPortal.Domain.Repositories;

/// <summary>Provides instructor eligibility persistence operations.</summary>
public interface IInstructorEligibilityRepository
{
    /// <summary>Gets tracked eligibility for one user and skill.</summary>
    Task<InstructorEligibility?> GetAsync(Guid userId, Guid skillId, CancellationToken cancellationToken = default);
    /// <summary>Lists a user's eligibility read-only.</summary>
    Task<IReadOnlyList<InstructorEligibility>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    /// <summary>Lists eligible records for a skill read-only.</summary>
    Task<IReadOnlyList<InstructorEligibility>> GetEligibleBySkillAsync(Guid skillId, CancellationToken cancellationToken = default);
    /// <summary>Checks current eligibility.</summary>
    Task<bool> IsEligibleAsync(Guid userId, Guid skillId, CancellationToken cancellationToken = default);
    /// <summary>Gets the user's best submitted qualifying attempt for a skill.</summary>
    Task<QuizAttempt?> GetBestAttemptAsync(Guid userId, Guid skillId, CancellationToken cancellationToken = default);
    /// <summary>Adds eligibility.</summary>
    Task AddAsync(InstructorEligibility eligibility, CancellationToken cancellationToken = default);
}
