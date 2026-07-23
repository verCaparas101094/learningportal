using LearningPortal.Domain.AiTutor;
namespace LearningPortal.Domain.Repositories;
/// <summary>Provides tutor conversation persistence.</summary>
public interface IAiTutorConversationRepository
{
    /// <summary>Gets a conversation graph.</summary>
    Task<AiTutorConversation?> GetAsync(Guid id,bool readOnly,CancellationToken cancellationToken=default);
    /// <summary>Lists learner conversations.</summary>
    Task<IReadOnlyList<AiTutorConversation>> GetByStudentAsync(Guid studentId,CancellationToken cancellationToken=default);
    /// <summary>Adds a conversation.</summary>
    Task AddAsync(AiTutorConversation conversation,CancellationToken cancellationToken=default);
}
