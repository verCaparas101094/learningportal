using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Courses.Commands.DeleteCourse;

/// <summary>Requests soft deletion of a Draft course.</summary>
public sealed record DeleteCourseCommand(Guid CourseId)
    : ICommand<Result<bool>>;
