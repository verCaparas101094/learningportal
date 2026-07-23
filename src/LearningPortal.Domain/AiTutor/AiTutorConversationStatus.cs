namespace LearningPortal.Domain.AiTutor;

/// <summary>Defines the lifecycle of an AI Tutor conversation.</summary>
public enum AiTutorConversationStatus
{
    /// <summary>The conversation accepts new learner messages.</summary>
    Active,

    /// <summary>The conversation is retained as read-only history.</summary>
    Archived
}
