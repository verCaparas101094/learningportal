#pragma warning disable CS1591
using LearningPortal.Domain.Quizzes;
using LearningPortal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LearningPortal.Infrastructure.Persistence.Repositories;

public sealed class QuizAttemptRepository(ApplicationDbContext context) : IQuizAttemptRepository
{
    public Task<QuizAttempt?> GetByIdAsync(Guid id, bool readOnly, CancellationToken cancellationToken = default)
    {
        var query = context.QuizAttempts.Include(attempt => attempt.Answers).AsQueryable();
        return (readOnly ? query.AsNoTracking() : query)
            .SingleOrDefaultAsync(attempt => attempt.Id == id, cancellationToken);
    }

    public Task<QuizAttempt?> GetActiveAsync(
        Guid quizId,
        Guid studentId,
        CancellationToken cancellationToken = default) =>
        context.QuizAttempts.Include(attempt => attempt.Answers).SingleOrDefaultAsync(
            attempt => attempt.QuizId == quizId
                && attempt.StudentId == studentId
                && attempt.Status == QuizAttemptStatus.InProgress,
            cancellationToken);

    public async Task<IReadOnlyList<QuizAttempt>> GetByQuizAndStudentAsync(
        Guid quizId,
        Guid studentId,
        CancellationToken cancellationToken = default) =>
        await context.QuizAttempts.AsNoTracking()
            .Where(attempt => attempt.QuizId == quizId && attempt.StudentId == studentId)
            .OrderByDescending(attempt => attempt.AttemptNumber)
            .ToListAsync(cancellationToken);

    public Task<int> CountAsync(Guid quizId, Guid studentId, CancellationToken cancellationToken = default) =>
        context.QuizAttempts.CountAsync(
            attempt => attempt.QuizId == quizId && attempt.StudentId == studentId,
            cancellationToken);

    public Task<bool> HasPassedAsync(Guid quizId, Guid enrollmentId, CancellationToken cancellationToken = default) =>
        context.QuizAttempts.AnyAsync(
            attempt => attempt.QuizId == quizId
                && attempt.EnrollmentId == enrollmentId
                && attempt.Status == QuizAttemptStatus.Submitted
                && attempt.Passed,
            cancellationToken);

    public Task AddAsync(QuizAttempt attempt, CancellationToken cancellationToken = default) =>
        context.QuizAttempts.AddAsync(attempt, cancellationToken).AsTask();
}
