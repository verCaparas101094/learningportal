using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Courses;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Courses.Queries.GetCourses;

/// <summary>Requests the available course catalog.</summary>
public sealed record GetCoursesQuery : IQuery<Result<IReadOnlyList<CourseDto>>>;
