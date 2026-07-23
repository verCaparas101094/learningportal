namespace LearningPortal.Shared.Lessons;

/// <summary>Contains fields required to update a Draft lesson.</summary>
public sealed record UpdateLessonRequest(
    string Title,
    string Description,
    string Content,
    int Order,
    int EstimatedMinutes,
    string LessonType,
    string RowVersion);
