namespace LearningPortal.Shared.Courses;

/// <summary>Represents a request to create a course.</summary>
/// <param name="Title">The course title.</param>
/// <param name="Description">The course description.</param>
public sealed record CreateCourseRequest(string Title, string Description);
