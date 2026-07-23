using LearningPortal.Domain.AiTutor;
using LearningPortal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LearningPortal.Infrastructure.Persistence.Repositories;

/// <summary>Persists and queries learner-owned AI Tutor conversations.</summary>
public sealed class AiTutorConversationRepository(ApplicationDbContext context)
    : IAiTutorConversationRepository
{
    /// <inheritdoc />
    public Task<AiTutorConversation?> GetAsync(
        Guid id,
        bool readOnly,
        CancellationToken cancellationToken = default)
    {
        var query = context.AiTutorConversations
            .Include(value => value.Messages)
            .AsQueryable();
        return (readOnly ? query.AsNoTracking() : query)
            .SingleOrDefaultAsync(value => value.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AiTutorConversation>> GetByStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default) =>
        await context.AiTutorConversations
            .AsNoTracking()
            .Where(value => value.StudentId == studentId)
            .OrderByDescending(value => value.LastMessageAtUtc)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task AddAsync(
        AiTutorConversation conversation,
        CancellationToken cancellationToken = default) =>
        context.AiTutorConversations.AddAsync(conversation, cancellationToken).AsTask();
}
