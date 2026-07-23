namespace LearningPortal.Shared.Courses;

/// <summary>Represents complete course-management data.</summary>
public sealed record CourseResponse(
    Guid Id,
    string Title,
    string Slug,
    string Description,
    string Category,
    string? ThumbnailUrl,
    string Status,
    Guid InstructorId,
    DateTimeOffset CreatedAtUtc,
    Guid? CreatedBy,
    DateTimeOffset? UpdatedAtUtc,
    Guid? UpdatedBy,
    string RowVersion);
