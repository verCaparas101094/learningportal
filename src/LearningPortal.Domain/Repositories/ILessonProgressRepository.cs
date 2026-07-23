using LearningPortal.Domain.Learning;

namespace LearningPortal.Domain.Repositories;

/// <summary>Provides persistence operations needed by learner progress.</summary>
public interface ILessonProgressRepository
{
    /// <summary>Gets tracked progress for an enrollment lesson.</summary>
    Task<LessonProgress?> GetByEnrollmentAndLessonAsync(Guid enrollmentId, Guid lessonId, CancellationToken cancellationToken = default);
    /// <summary>Gets read-only progress records for an enrollment.</summary>
    Task<IReadOnlyList<LessonProgress>> GetByEnrollmentAsync(Guid enrollmentId, CancellationToken cancellationToken = default);
    /// <summary>Adds a new progress record.</summary>
    Task AddAsync(LessonProgress progress, CancellationToken cancellationToken = default);
    /// <summary>Sets the concurrency original value.</summary>
    void SetOriginalRowVersion(LessonProgress progress, byte[] rowVersion);
}
