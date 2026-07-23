using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LearningPortal.Infrastructure.Persistence.Repositories;

/// <summary>Implements course-specific EF Core persistence.</summary>
public sealed class CourseRepository(ApplicationDbContext dbContext) : ICourseRepository
{
    /// <inheritdoc />
    public Task<Course?> GetByIdAsync(
        Guid courseId,
        CancellationToken cancellationToken = default) =>
        dbContext.Courses.SingleOrDefaultAsync(course => course.Id == courseId, cancellationToken);

    /// <inheritdoc />
    public Task<Course?> GetByIdReadOnlyAsync(
        Guid courseId,
        CancellationToken cancellationToken = default) =>
        dbContext.Courses
            .AsNoTracking()
            .SingleOrDefaultAsync(course => course.Id == courseId, cancellationToken);

    /// <inheritdoc />
    public Task<Course?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        dbContext.Courses.AsNoTracking().SingleOrDefaultAsync(
            course => course.Slug == slug && course.Status == CourseStatus.Published,
            cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Course>> GetByIdsReadOnlyAsync(
        IReadOnlyCollection<Guid> courseIds,
        CancellationToken cancellationToken = default) =>
        await dbContext.Courses.AsNoTracking()
            .Where(course => courseIds.Contains(course.Id))
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Course> Items, int TotalCount)> GetPageAsync(
        string? search,
        CourseStatus? status,
        Guid? instructorId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Courses.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(course =>
                course.Title.Contains(term)
                || course.Slug.Contains(term)
                || course.Category.Contains(term));
        }

        if (status is not null)
        {
            query = query.Where(course => course.Status == status);
        }

        if (instructorId is not null)
        {
            query = query.Where(course => course.InstructorId == instructorId);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(course => course.CreatedAtUtc)
            .ThenBy(course => course.Title)
            .ThenBy(course => course.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc />
    public Task<bool> SlugExistsAsync(
        string slug,
        Guid? excludedCourseId = null,
        CancellationToken cancellationToken = default) =>
        dbContext.Courses.AnyAsync(
            course => course.Slug == slug
                      && (excludedCourseId == null || course.Id != excludedCourseId),
            cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(Course course, CancellationToken cancellationToken = default) =>
        await dbContext.Courses.AddAsync(course, cancellationToken);

    /// <inheritdoc />
    public void Remove(Course course) => dbContext.Courses.Remove(course);

    /// <inheritdoc />
    public void SetOriginalRowVersion(Course course, byte[] rowVersion)
    {
        ArgumentNullException.ThrowIfNull(rowVersion);
        dbContext.Entry(course).Property(entity => entity.RowVersion).OriginalValue = rowVersion;
    }
}
