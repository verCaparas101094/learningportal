using LearningPortal.Domain.Quizzes;

namespace LearningPortal.Domain.Repositories;

/// <summary>Provides owned quiz-attempt persistence operations.</summary>
public interface IQuizAttemptRepository
{
    /// <summary>Gets an attempt and answer snapshots.</summary>
    Task<QuizAttempt?> GetByIdAsync(Guid id, bool readOnly, CancellationToken cancellationToken = default);

    /// <summary>Gets the student's active attempt for a quiz.</summary>
    Task<QuizAttempt?> GetActiveAsync(Guid quizId, Guid studentId, CancellationToken cancellationToken = default);

    /// <summary>Gets a student's quiz attempts.</summary>
    Task<IReadOnlyList<QuizAttempt>> GetByQuizAndStudentAsync(
        Guid quizId,
        Guid studentId,
        CancellationToken cancellationToken = default);

    /// <summary>Gets the number of started attempts.</summary>
    Task<int> CountAsync(Guid quizId, Guid studentId, CancellationToken cancellationToken = default);

    /// <summary>Checks whether an enrollment passed a quiz.</summary>
    Task<bool> HasPassedAsync(Guid quizId, Guid enrollmentId, CancellationToken cancellationToken = default);

    /// <summary>Adds an attempt.</summary>
    Task AddAsync(QuizAttempt attempt, CancellationToken cancellationToken = default);
}
