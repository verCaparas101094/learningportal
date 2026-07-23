#pragma warning disable CS1591
using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Lessons;
using LearningPortal.Shared.Results;
namespace LearningPortal.Application.Lessons.Queries.GetLessonsByCourse;
public sealed class GetLessonsByCourseQueryHandler(ILessonRepository lessons, ICourseRepository courses,
    ICurrentUserService user) : IQueryHandler<GetLessonsByCourseQuery, Result<PagedLessonsResponse>>
{
    public async Task<Result<PagedLessonsResponse>> HandleAsync(GetLessonsByCourseQuery q, CancellationToken ct = default)
    {
        var course = await courses.GetByIdReadOnlyAsync(q.CourseId, ct);
        if (course is null) return Result<PagedLessonsResponse>.Failure(Errors.CourseManagement.NotFound(q.CourseId));
        var error = LessonSupport.Authorize(user, course);
        if (error is not null) return Result<PagedLessonsResponse>.Failure(error);
        var page = await lessons.GetPageAsync(q.CourseId, q.Search, q.PageNumber, q.PageSize, cancellationToken: ct);
        return Result<PagedLessonsResponse>.Success(new(page.Items.Select(x => x.ToListItem()).ToArray(),
            q.PageNumber, q.PageSize, page.TotalCount,
            (int)Math.Ceiling(page.TotalCount / (double)q.PageSize)));
    }
}
