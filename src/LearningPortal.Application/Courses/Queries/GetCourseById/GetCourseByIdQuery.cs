using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Courses;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Courses.Queries.GetCourseById;

/// <summary>Requests one authorized course by identifier.</summary>
public sealed record GetCourseByIdQuery(Guid CourseId)
    : IQuery<Result<CourseResponse>>;
