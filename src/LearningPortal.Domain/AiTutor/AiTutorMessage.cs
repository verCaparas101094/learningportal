using LearningPortal.Domain.Common;

namespace LearningPortal.Domain.AiTutor;

/// <summary>Represents one ordered visible tutor message.</summary>
public sealed class AiTutorMessage : Entity
{
    private AiTutorMessage()
    {
    }

    private AiTutorMessage(
        Guid conversationId,
        AiTutorMessageRole role,
        string content,
        DateTimeOffset createdAtUtc,
        int sequence)
    {
        ConversationId = conversationId;
        Role = role;
        Content = content;
        CreatedAtUtc = createdAtUtc;
        Sequence = sequence;
    }

    /// <summary>Gets the conversation.</summary>
    public Guid ConversationId { get; private set; }

    /// <summary>Gets the visible author role.</summary>
    public AiTutorMessageRole Role { get; private set; }

    /// <summary>Gets message content.</summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>Gets creation time.</summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>Gets stable order.</summary>
    public int Sequence { get; private set; }

    internal static AiTutorMessage Create(
        Guid conversationId,
        AiTutorMessageRole role,
        string content,
        DateTimeOffset createdAtUtc,
        int sequence)
    {
        if (conversationId == Guid.Empty
            || string.IsNullOrWhiteSpace(content)
            || sequence < 1
            || createdAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Message values are invalid.");
        }

        return new AiTutorMessage(conversationId, role, content.Trim(), createdAtUtc, sequence);
    }
}
