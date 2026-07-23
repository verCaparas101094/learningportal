namespace LearningPortal.Shared.Courses;

/// <summary>Models creation of a Draft course.</summary>
public sealed record CreateCourseRequest(
    string Title,
    string Slug,
    string Description,
    string Category,
    string? ThumbnailUrl,
    Guid? InstructorId);
