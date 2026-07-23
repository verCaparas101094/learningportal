using LearningPortal.Domain.Courses;

namespace LearningPortal.Domain.Repositories;

/// <summary>Defines persistence operations required by course management.</summary>
public interface ICourseRepository
{
    /// <summary>Returns a tracked course by identifier.</summary>
    Task<Course?> GetByIdAsync(Guid courseId, CancellationToken cancellationToken = default);

    /// <summary>Returns an untracked course by identifier.</summary>
    Task<Course?> GetByIdReadOnlyAsync(Guid courseId, CancellationToken cancellationToken = default);

    /// <summary>Returns an untracked published course by slug.</summary>
    Task<Course?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Returns untracked courses matching identifiers.</summary>
    Task<IReadOnlyList<Course>> GetByIdsReadOnlyAsync(
        IReadOnlyCollection<Guid> courseIds,
        CancellationToken cancellationToken = default);

    /// <summary>Returns a filtered course page and total matching count.</summary>
    Task<(IReadOnlyList<Course> Items, int TotalCount)> GetPageAsync(
        string? search,
        CourseStatus? status,
        Guid? instructorId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Determines whether a non-deleted course owns a normalized slug.</summary>
    Task<bool> SlugExistsAsync(
        string slug,
        Guid? excludedCourseId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Adds a course.</summary>
    Task AddAsync(Course course, CancellationToken cancellationToken = default);

    /// <summary>Marks a course for soft deletion.</summary>
    void Remove(Course course);

    /// <summary>Sets the client-provided original rowversion used for concurrency checks.</summary>
    void SetOriginalRowVersion(Course course, byte[] rowVersion);
}
