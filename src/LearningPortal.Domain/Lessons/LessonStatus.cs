namespace LearningPortal.Domain.Lessons;

/// <summary>Defines lesson lifecycle states.</summary>
public enum LessonStatus
{
    /// <summary>Editable draft.</summary>
    Draft,
    /// <summary>Published read-only lesson.</summary>
    Published
}
