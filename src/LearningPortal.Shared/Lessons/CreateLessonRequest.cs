namespace LearningPortal.Shared.Lessons;

/// <summary>Contains fields required to create a lesson.</summary>
public sealed record CreateLessonRequest(
    string Title,
    string Description,
    string Content,
    int Order,
    int EstimatedMinutes,
    string LessonType);
