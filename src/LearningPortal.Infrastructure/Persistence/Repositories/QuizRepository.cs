#pragma warning disable CS1591
using LearningPortal.Domain.Quizzes;
using LearningPortal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
namespace LearningPortal.Infrastructure.Persistence.Repositories;
public sealed class QuizRepository(ApplicationDbContext context) : IQuizRepository
{
    public Task<Quiz?> GetByIdAsync(Guid id, CancellationToken ct = default) => context.Quizzes.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<Quiz?> GetByIdReadOnlyAsync(Guid id, CancellationToken ct = default) => context.Quizzes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<Quiz?> GetGraphAsync(Guid id, CancellationToken ct = default) => context.Quizzes.Include(x => x.Questions).ThenInclude(x => x.AnswerChoices).SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<IReadOnlyList<Quiz>> GetByCourseAsync(Guid courseId, CancellationToken ct = default) =>
        await context.Quizzes.AsNoTracking()
            .Include(x => x.Questions).ThenInclude(x => x.AnswerChoices)
            .Where(x => x.CourseId == courseId).OrderBy(x => x.CreatedAtUtc).ToListAsync(ct);
    public async Task<IReadOnlyList<Quiz>> GetRequiredPublishedByCourseAsync(Guid courseId, CancellationToken ct = default) =>
        await context.Quizzes.AsNoTracking()
            .Where(x => x.CourseId == courseId && x.Status == QuizStatus.Published && x.IsRequiredForCourseCompletion)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(ct);
    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) => context.Quizzes.AnyAsync(x => x.Id == id, ct);
    public Task AddAsync(Quiz quiz, CancellationToken ct = default) => context.Quizzes.AddAsync(quiz, ct).AsTask();
    public void SetOriginalRowVersion(Quiz quiz, byte[] value) => context.Entry(quiz).Property(x => x.RowVersion).OriginalValue = value;
}
