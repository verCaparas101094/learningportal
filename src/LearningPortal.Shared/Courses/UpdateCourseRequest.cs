namespace LearningPortal.Shared.Courses;

/// <summary>Models an optimistic-concurrency protected Draft course update.</summary>
public sealed record UpdateCourseRequest(
    string Title,
    string Slug,
    string Description,
    string Category,
    string? ThumbnailUrl,
    string RowVersion);
