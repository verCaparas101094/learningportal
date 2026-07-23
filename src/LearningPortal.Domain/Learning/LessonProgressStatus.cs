namespace LearningPortal.Domain.Learning;

/// <summary>Defines learner progress through a published lesson.</summary>
public enum LessonProgressStatus
{
    /// <summary>Lesson has not yet been opened.</summary>
    NotStarted,
    /// <summary>Lesson has been opened but not completed.</summary>
    InProgress,
    /// <summary>Lesson has been completed.</summary>
    Completed
}
