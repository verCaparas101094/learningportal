using LearningPortal.Domain.Quizzes;

namespace LearningPortal.Domain.Repositories;

/// <summary>Provides quiz persistence operations.</summary>
public interface IQuizRepository
{
    /// <summary>Gets a tracked quiz.</summary>
    Task<Quiz?> GetByIdAsync(Guid quizId, CancellationToken cancellationToken = default);
    /// <summary>Gets a read-only quiz.</summary>
    Task<Quiz?> GetByIdReadOnlyAsync(Guid quizId, CancellationToken cancellationToken = default);
    /// <summary>Gets a quiz and ordered authoring graph.</summary>
    Task<Quiz?> GetGraphAsync(Guid quizId, CancellationToken cancellationToken = default);
    /// <summary>Gets read-only course quizzes.</summary>
    Task<IReadOnlyList<Quiz>> GetByCourseAsync(Guid courseId, CancellationToken cancellationToken = default);
    /// <summary>Gets required published quizzes for course completion.</summary>
    Task<IReadOnlyList<Quiz>> GetRequiredPublishedByCourseAsync(
        Guid courseId,
        CancellationToken cancellationToken = default);
    /// <summary>Checks quiz existence.</summary>
    Task<bool> ExistsAsync(Guid quizId, CancellationToken cancellationToken = default);
    /// <summary>Adds a quiz.</summary>
    Task AddAsync(Quiz quiz, CancellationToken cancellationToken = default);
    /// <summary>Sets optimistic concurrency state.</summary>
    void SetOriginalRowVersion(Quiz quiz, byte[] rowVersion);
}
