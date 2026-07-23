namespace LearningPortal.Shared.Lessons;

/// <summary>Represents one page of lessons.</summary>
public sealed record PagedLessonsResponse(
    IReadOnlyList<LessonListItemResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);
