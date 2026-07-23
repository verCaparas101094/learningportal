using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Courses;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Courses.Commands.UpdateCourse;

/// <summary>Requests an optimistic-concurrency protected Draft course update.</summary>
public sealed record UpdateCourseCommand(
    Guid CourseId,
    string Title,
    string Slug,
    string Description,
    string Category,
    string? ThumbnailUrl,
    string RowVersion)
    : ICommand<Result<CourseResponse>>;
