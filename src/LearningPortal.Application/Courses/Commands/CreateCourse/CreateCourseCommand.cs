using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Courses;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Courses.Commands.CreateCourse;

/// <summary>Requests creation of a course.</summary>
/// <param name="Title">The course title.</param>
/// <param name="Description">The course description.</param>
public sealed record CreateCourseCommand(string Title, string Description) : ICommand<Result<CourseDto>>;
