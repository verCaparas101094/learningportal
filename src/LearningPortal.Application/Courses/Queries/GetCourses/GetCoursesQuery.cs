using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Courses;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Courses.Queries.GetCourses;

/// <summary>Requests one authorized, filtered page of courses.</summary>
public sealed record GetCoursesQuery(
    string? Search,
    string? Status,
    int PageNumber,
    int PageSize)
    : IQuery<Result<PagedCoursesResponse>>;
