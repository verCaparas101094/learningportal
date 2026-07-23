namespace LearningPortal.Shared.Courses;

/// <summary>Represents one compact course-list item.</summary>
public sealed record CourseListItemResponse(
    Guid Id,
    string Title,
    string Slug,
    string Category,
    string? ThumbnailUrl,
    string Status,
    Guid InstructorId,
    DateTimeOffset CreatedAtUtc,
    Guid? SkillId = null);
