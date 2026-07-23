namespace LearningPortal.Domain.AiTutor;

/// <summary>Defines persisted, user-visible AI Tutor message authors.</summary>
public enum AiTutorMessageRole
{
    /// <summary>The learner authored the message.</summary>
    User,

    /// <summary>The local AI Tutor authored the message.</summary>
    Assistant
}
