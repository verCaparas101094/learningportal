namespace LearningPortal.Shared.Courses;

/// <summary>Represents course data exposed across process boundaries.</summary>
/// <param name="Id">The course identifier.</param>
/// <param name="Title">The display title.</param>
/// <param name="Description">The course description.</param>
/// <param name="CreatedAtUtc">The UTC creation timestamp.</param>
public sealed record CourseDto(Guid Id, string Title, string Description, DateTimeOffset CreatedAtUtc);
