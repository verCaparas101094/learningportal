using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Courses;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Courses.Commands.ArchiveCourse;

/// <summary>Requests archival of a Published course.</summary>
public sealed record ArchiveCourseCommand(Guid CourseId)
    : ICommand<Result<CourseResponse>>;
