namespace LearningPortal.Shared.Lessons;

/// <summary>Represents a lesson list row.</summary>
public sealed record LessonListItemResponse(
    Guid Id,
    Guid CourseId,
    string Title,
    int Order,
    int EstimatedMinutes,
    string LessonType,
    string Status,
    string RowVersion);
