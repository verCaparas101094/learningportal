#pragma warning disable CS1591
using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Quizzes;
using LearningPortal.Domain.Repositories;
using LearningPortal.Domain.Skills;
using Microsoft.EntityFrameworkCore;

namespace LearningPortal.Infrastructure.Persistence.Repositories;

public sealed class SkillRepository(ApplicationDbContext context) : ISkillRepository
{
    public Task<Skill?> GetByIdAsync(Guid id, bool readOnly = true, CancellationToken ct = default)
    {
        var query = context.Skills.Where(skill => skill.IsActive);
        return (readOnly ? query.AsNoTracking() : query).SingleOrDefaultAsync(skill => skill.Id == id, ct);
    }
    public Task<Skill?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        context.Skills.SingleOrDefaultAsync(skill => skill.Slug == slug, ct);
    public Task<Skill?> GetByNameAsync(string name, CancellationToken ct = default) =>
        context.Skills.SingleOrDefaultAsync(skill => skill.Name == name.Trim(), ct);
    public async Task<IReadOnlyList<Skill>> GetActiveAsync(CancellationToken ct = default) =>
        await context.Skills.AsNoTracking().Where(skill => skill.IsActive).OrderBy(skill => skill.Name).ToListAsync(ct);
    public Task AddAsync(Skill skill, CancellationToken ct = default) => context.Skills.AddAsync(skill, ct).AsTask();
}

public sealed class InstructorEligibilityRepository(ApplicationDbContext context) : IInstructorEligibilityRepository
{
    public Task<InstructorEligibility?> GetAsync(Guid userId, Guid skillId, CancellationToken ct = default) =>
        context.InstructorEligibility.SingleOrDefaultAsync(
            value => value.UserId == userId && value.SkillId == skillId, ct);
    public async Task<IReadOnlyList<InstructorEligibility>> GetByUserAsync(Guid userId, CancellationToken ct = default) =>
        await context.InstructorEligibility.AsNoTracking().Where(value => value.UserId == userId)
            .OrderBy(value => value.SkillId).ToListAsync(ct);
    public async Task<IReadOnlyList<InstructorEligibility>> GetEligibleBySkillAsync(Guid skillId, CancellationToken ct = default) =>
        await context.InstructorEligibility.AsNoTracking()
            .Where(value => value.SkillId == skillId && value.IsEligible)
            .OrderByDescending(value => value.BestPercentage).ToListAsync(ct);
    public Task<bool> IsEligibleAsync(Guid userId, Guid skillId, CancellationToken ct = default) =>
        context.InstructorEligibility.AnyAsync(
            value => value.UserId == userId && value.SkillId == skillId && value.IsEligible, ct);
    public Task<QuizAttempt?> GetBestAttemptAsync(Guid userId, Guid skillId, CancellationToken ct = default) =>
        context.QuizAttempts.AsNoTracking()
            .Where(attempt => attempt.StudentId == userId
                && attempt.Status == QuizAttemptStatus.Submitted
                && attempt.Passed
                && context.Quizzes.Any(quiz => quiz.Id == attempt.QuizId
                    && quiz.Status == QuizStatus.Published
                    && quiz.IsInstructorAssessment
                    && quiz.SkillId == skillId))
            .OrderByDescending(attempt => attempt.Percentage).FirstOrDefaultAsync(ct);
    public Task AddAsync(InstructorEligibility eligibility, CancellationToken ct = default) =>
        context.InstructorEligibility.AddAsync(eligibility, ct).AsTask();
}
