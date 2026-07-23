using LearningPortal.Domain.Lessons;
using LearningPortal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LearningPortal.Infrastructure.Persistence.Repositories;

/// <summary>Persists and queries lessons.</summary>
public sealed class LessonRepository(ApplicationDbContext context) : ILessonRepository
{
    /// <inheritdoc />
    public Task<Lesson?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        context.Lessons.SingleOrDefaultAsync(x => x.Id == id, ct);
    /// <inheritdoc />
    public Task<Lesson?> GetByIdReadOnlyAsync(Guid id, CancellationToken ct = default) =>
        context.Lessons.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
    /// <inheritdoc />
    public async Task<(IReadOnlyList<Lesson> Items, int TotalCount)> GetPageAsync(
        Guid? courseId, string? search, int pageNumber, int pageSize, Guid? instructorId = null, CancellationToken ct = default)
    {
        var query = context.Lessons.AsNoTracking().Where(x => !courseId.HasValue || x.CourseId == courseId);
        if (instructorId.HasValue)
        {
            query = query.Where(x => context.Courses.Any(c => c.Id == x.CourseId && c.InstructorId == instructorId));
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Title.Contains(term) || x.Description.Contains(term));
        }
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.CourseId).ThenBy(x => x.Order)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }
    /// <inheritdoc />
    public Task<bool> OrderExistsAsync(Guid courseId, int order, Guid? excludedLessonId = null, CancellationToken ct = default) =>
        context.Lessons.AnyAsync(x => x.CourseId == courseId && x.Order == order &&
            (!excludedLessonId.HasValue || x.Id != excludedLessonId), ct);
    /// <inheritdoc />
    public Task AddAsync(Lesson lesson, CancellationToken ct = default) =>
        context.Lessons.AddAsync(lesson, ct).AsTask();
    /// <inheritdoc />
    public void Remove(Lesson lesson) => context.Lessons.Remove(lesson);
    /// <inheritdoc />
    public void SetOriginalRowVersion(Lesson lesson, byte[] rowVersion) =>
        context.Entry(lesson).Property(x => x.RowVersion).OriginalValue = rowVersion;
    /// <inheritdoc />
    public async Task<LessonMoveResult> MoveAsync(Guid lessonId, int newOrder, byte[] rowVersion, CancellationToken ct = default)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(ct);
            var lesson = await context.Lessons.SingleOrDefaultAsync(x => x.Id == lessonId, ct);
            if (lesson is null) return LessonMoveResult.NotFound;
            if (!lesson.RowVersion.SequenceEqual(rowVersion)) return LessonMoveResult.ConcurrencyConflict;
            if (lesson.Status != LessonStatus.Draft || newOrder < 1) return LessonMoveResult.InvalidOrder;
            var other = await context.Lessons.SingleOrDefaultAsync(
                x => x.CourseId == lesson.CourseId && x.Order == newOrder && x.Status == LessonStatus.Draft, ct);
            if (other is null) return LessonMoveResult.InvalidOrder;
            var oldOrder = lesson.Order;
            var affected = await context.Lessons.Where(x => x.Id == lesson.Id || x.Id == other.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    x => x.Order, x => x.Id == lesson.Id ? newOrder : oldOrder), ct);
            if (affected != 2) return LessonMoveResult.ConcurrencyConflict;
            await transaction.CommitAsync(ct);
            context.ChangeTracker.Clear();
            return LessonMoveResult.Moved;
        });
    }
}
