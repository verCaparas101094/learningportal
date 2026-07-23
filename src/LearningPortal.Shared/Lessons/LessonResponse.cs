namespace LearningPortal.Shared.Lessons;

/// <summary>Represents complete lesson details.</summary>
public sealed record LessonResponse(
    Guid Id,
    Guid CourseId,
    string Title,
    string Description,
    string Content,
    int Order,
    int EstimatedMinutes,
    string LessonType,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    string RowVersion);
