using LearningPortal.Domain.Enrollments;

namespace LearningPortal.Domain.Repositories;

/// <summary>Defines enrollment persistence operations.</summary>
public interface IEnrollmentRepository
{
    /// <summary>Gets a tracked enrollment.</summary>
    Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Gets an untracked enrollment.</summary>
    Task<Enrollment?> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Gets a student's enrollment for a course.</summary>
    Task<Enrollment?> GetByCourseAndStudentAsync(
        Guid courseId,
        Guid studentId,
        CancellationToken cancellationToken = default);
    /// <summary>Gets a filtered student enrollment page.</summary>
    Task<(IReadOnlyList<Enrollment> Items, int TotalCount)> GetStudentPageAsync(
        Guid studentId,
        EnrollmentStatus? status,
        string? search,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    /// <summary>Gets a filtered course enrollment page.</summary>
    Task<(IReadOnlyList<Enrollment> Items, int TotalCount)> GetCoursePageAsync(
        Guid courseId,
        EnrollmentStatus? status,
        string? search,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    /// <summary>Gets active enrollment course identifiers for a student.</summary>
    Task<IReadOnlySet<Guid>> GetActiveCourseIdsAsync(
        Guid studentId,
        IReadOnlyCollection<Guid> courseIds,
        CancellationToken cancellationToken = default);
    /// <summary>Gets active enrollments for selected courses.</summary>
    Task<IReadOnlyList<Enrollment>> GetActiveByStudentAndCoursesAsync(
        Guid studentId,
        IReadOnlyCollection<Guid> courseIds,
        CancellationToken cancellationToken = default);
    /// <summary>Adds an enrollment.</summary>
    Task AddAsync(Enrollment enrollment, CancellationToken cancellationToken = default);
    /// <summary>Sets the expected concurrency token.</summary>
    void SetOriginalRowVersion(Enrollment enrollment, byte[] rowVersion);
}
