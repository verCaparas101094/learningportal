using LearningPortal.Domain.Common;

namespace LearningPortal.Domain.AiTutor;

/// <summary>Represents a learner-owned course tutor conversation.</summary>
public sealed class AiTutorConversation : AuditableEntity
{
    private readonly List<AiTutorMessage> _messages = [];

    private AiTutorConversation()
    {
    }

    private AiTutorConversation(
        Guid studentId,
        Guid courseId,
        Guid? lessonId,
        string title,
        DateTimeOffset createdAtUtc)
    {
        StudentId = studentId;
        CourseId = courseId;
        LessonId = lessonId;
        Title = title;
        Status = AiTutorConversationStatus.Active;
        LastMessageAtUtc = createdAtUtc;
    }

    /// <summary>Gets the learner owner.</summary>
    public Guid StudentId { get; private set; }

    /// <summary>Gets the course scope.</summary>
    public Guid CourseId { get; private set; }

    /// <summary>Gets the optional lesson scope.</summary>
    public Guid? LessonId { get; private set; }

    /// <summary>Gets the visible title.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Gets lifecycle status.</summary>
    public AiTutorConversationStatus Status { get; private set; }

    /// <summary>Gets last activity time.</summary>
    public DateTimeOffset LastMessageAtUtc { get; private set; }

    /// <summary>Gets ordered visible messages.</summary>
    public IReadOnlyCollection<AiTutorMessage> Messages => _messages.AsReadOnly();

    /// <summary>Starts an active conversation.</summary>
    public static AiTutorConversation Start(
        Guid studentId,
        Guid courseId,
        Guid? lessonId,
        string title,
        DateTimeOffset createdAtUtc)
    {
        if (studentId == Guid.Empty
            || courseId == Guid.Empty
            || string.IsNullOrWhiteSpace(title)
            || title.Trim().Length > 200
            || createdAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Conversation values are invalid.");
        }

        return new AiTutorConversation(studentId, courseId, lessonId, title.Trim(), createdAtUtc);
    }

    /// <summary>Adds a complete user/reply pair atomically to the aggregate.</summary>
    public bool TryAddExchange(string question, string reply, DateTimeOffset createdAtUtc)
    {
        if (Status != AiTutorConversationStatus.Active
            || string.IsNullOrWhiteSpace(question)
            || string.IsNullOrWhiteSpace(reply)
            || createdAtUtc.Offset != TimeSpan.Zero)
        {
            return false;
        }

        var sequence = _messages.Count + 1;
        _messages.Add(AiTutorMessage.Create(
            Id, AiTutorMessageRole.User, question, createdAtUtc, sequence));
        _messages.Add(AiTutorMessage.Create(
            Id, AiTutorMessageRole.Assistant, reply, createdAtUtc, sequence + 1));
        LastMessageAtUtc = createdAtUtc;
        return true;
    }

    /// <summary>Archives idempotently.</summary>
    public bool TryArchive()
    {
        Status = AiTutorConversationStatus.Archived;
        return true;
    }
}
