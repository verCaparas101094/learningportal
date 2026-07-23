#pragma warning disable CS1591
using LearningPortal.Domain.Learning;
using LearningPortal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LearningPortal.Infrastructure.Persistence.Repositories;

/// <summary>Persists learner lesson progress.</summary>
public sealed class LessonProgressRepository(ApplicationDbContext context) : ILessonProgressRepository
{
    public Task<LessonProgress?> GetByEnrollmentAndLessonAsync(Guid enrollmentId, Guid lessonId, CancellationToken cancellationToken = default) =>
        context.Set<LessonProgress>().SingleOrDefaultAsync(x => x.EnrollmentId == enrollmentId && x.LessonId == lessonId, cancellationToken);
    public async Task<IReadOnlyList<LessonProgress>> GetByEnrollmentAsync(Guid enrollmentId, CancellationToken cancellationToken = default) =>
        await context.Set<LessonProgress>().AsNoTracking().Where(x => x.EnrollmentId == enrollmentId).ToListAsync(cancellationToken);
    public Task AddAsync(LessonProgress progress, CancellationToken cancellationToken = default) => context.Set<LessonProgress>().AddAsync(progress, cancellationToken).AsTask();
    public void SetOriginalRowVersion(LessonProgress progress, byte[] rowVersion) => context.Entry(progress).Property(x => x.RowVersion).OriginalValue = rowVersion;
}
