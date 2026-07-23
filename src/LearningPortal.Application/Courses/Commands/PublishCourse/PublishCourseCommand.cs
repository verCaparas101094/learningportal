using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Courses;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Courses.Commands.PublishCourse;

/// <summary>Requests publication of a Draft course.</summary>
public sealed record PublishCourseCommand(Guid CourseId)
    : ICommand<Result<CourseResponse>>;
