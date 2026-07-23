using LearningPortal.Domain.Lessons;

namespace LearningPortal.Domain.Repositories;

/// <summary>Defines lesson-management persistence operations.</summary>
public interface ILessonRepository
{
    /// <summary>Gets a tracked lesson.</summary>
    Task<Lesson?> GetByIdAsync(Guid lessonId, CancellationToken cancellationToken = default);
    /// <summary>Gets a read-only lesson.</summary>
    Task<Lesson?> GetByIdReadOnlyAsync(Guid lessonId, CancellationToken cancellationToken = default);
    /// <summary>Gets a filtered page.</summary>
    Task<(IReadOnlyList<Lesson> Items, int TotalCount)> GetPageAsync(
        Guid? courseId,
        string? search,
        int pageNumber,
        int pageSize,
        Guid? instructorId = null,
        CancellationToken cancellationToken = default);
    /// <summary>Checks active course order uniqueness.</summary>
    Task<bool> OrderExistsAsync(
        Guid courseId,
        int order,
        Guid? excludedLessonId = null,
        CancellationToken cancellationToken = default);
    /// <summary>Adds a lesson.</summary>
    Task AddAsync(Lesson lesson, CancellationToken cancellationToken = default);
    /// <summary>Removes a lesson through soft delete.</summary>
    void Remove(Lesson lesson);
    /// <summary>Sets the client concurrency value.</summary>
    void SetOriginalRowVersion(Lesson lesson, byte[] rowVersion);
    /// <summary>Atomically swaps a lesson with a target order.</summary>
    Task<LessonMoveResult> MoveAsync(
        Guid lessonId,
        int newOrder,
        byte[] rowVersion,
        CancellationToken cancellationToken = default);
}

/// <summary>Describes an atomic lesson reorder result.</summary>
public enum LessonMoveResult
{
    /// <summary>Move completed.</summary>
    Moved,
    /// <summary>Lesson was missing.</summary>
    NotFound,
    /// <summary>Target order was invalid.</summary>
    InvalidOrder,
    /// <summary>Lesson changed concurrently.</summary>
    ConcurrencyConflict
}
