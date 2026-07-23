using LearningPortal.Domain.Enrollments;
using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LearningPortal.Infrastructure.Persistence.Repositories;

/// <summary>Implements enrollment persistence with EF Core.</summary>
public sealed class EnrollmentRepository(ApplicationDbContext dbContext) : IEnrollmentRepository
{
    /// <inheritdoc />
    public Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Enrollments.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<Enrollment?> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Enrollments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<Enrollment?> GetByCourseAndStudentAsync(
        Guid courseId, Guid studentId, CancellationToken cancellationToken = default) =>
        dbContext.Enrollments.SingleOrDefaultAsync(
            x => x.CourseId == courseId && x.StudentId == studentId && x.Status != EnrollmentStatus.Withdrawn,
            cancellationToken);

    /// <inheritdoc />
    public Task<(IReadOnlyList<Enrollment> Items, int TotalCount)> GetStudentPageAsync(
        Guid studentId, EnrollmentStatus? status, string? search, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default) =>
        PageAsync(FilterStudent(studentId, status, search), pageNumber, pageSize, cancellationToken);

    /// <inheritdoc />
    public Task<(IReadOnlyList<Enrollment> Items, int TotalCount)> GetCoursePageAsync(
        Guid courseId, EnrollmentStatus? status, string? search, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default) =>
        PageAsync(FilterCourse(courseId, status, search), pageNumber, pageSize, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> GetActiveCourseIdsAsync(
        Guid studentId, IReadOnlyCollection<Guid> courseIds, CancellationToken cancellationToken = default) =>
        (await dbContext.Enrollments.AsNoTracking()
            .Where(x => x.StudentId == studentId && x.Status != EnrollmentStatus.Withdrawn
                        && courseIds.Contains(x.CourseId))
            .Select(x => x.CourseId).ToListAsync(cancellationToken)).ToHashSet();

    /// <inheritdoc />
    public async Task<IReadOnlyList<Enrollment>> GetActiveByStudentAndCoursesAsync(
        Guid studentId, IReadOnlyCollection<Guid> courseIds, CancellationToken cancellationToken = default) =>
        await dbContext.Enrollments.AsNoTracking()
            .Where(x => x.StudentId == studentId && x.Status != EnrollmentStatus.Withdrawn
                        && courseIds.Contains(x.CourseId))
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(Enrollment enrollment, CancellationToken cancellationToken = default) =>
        await dbContext.Enrollments.AddAsync(enrollment, cancellationToken);

    /// <inheritdoc />
    public void SetOriginalRowVersion(Enrollment enrollment, byte[] rowVersion) =>
        dbContext.Entry(enrollment).Property(x => x.RowVersion).OriginalValue = rowVersion;

    private IQueryable<Enrollment> FilterStudent(Guid studentId, EnrollmentStatus? status, string? search)
    {
        var query = dbContext.Enrollments.AsNoTracking().Where(x =>
            x.StudentId == studentId
            && dbContext.Courses.Any(c => c.Id == x.CourseId && c.Status == CourseStatus.Published));
        if (status is not null) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => dbContext.Courses.Any(c => c.Id == x.CourseId
                && (c.Title.Contains(term) || c.Category.Contains(term))));
        }
        return query;
    }

    private IQueryable<Enrollment> FilterCourse(Guid courseId, EnrollmentStatus? status, string? search)
    {
        var query = dbContext.Enrollments.AsNoTracking().Where(x => x.CourseId == courseId);
        if (status is not null) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => dbContext.Users.Any(u => u.Id == x.StudentId
                && (u.DisplayName.Contains(term) || (u.Email != null && u.Email.Contains(term)))));
        }
        return query;
    }

    private static async Task<(IReadOnlyList<Enrollment>, int)> PageAsync(
        IQueryable<Enrollment> query, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.EnrolledAtUtc).ThenBy(x => x.Id)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, totalCount);
    }
}
