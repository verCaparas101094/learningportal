#pragma warning disable CS1591
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Lessons;
using LearningPortal.Shared.Results;
namespace LearningPortal.Application.Lessons.Queries.GetLessonsByCourse;
public sealed record GetLessonsByCourseQuery(Guid CourseId, string? Search, int PageNumber, int PageSize)
    : IQuery<Result<PagedLessonsResponse>>;
