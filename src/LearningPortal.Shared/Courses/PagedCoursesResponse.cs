namespace LearningPortal.Shared.Courses;

/// <summary>Contains one filtered page of courses.</summary>
public sealed record PagedCoursesResponse(
    IReadOnlyCollection<CourseListItemResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);
