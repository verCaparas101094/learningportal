#pragma warning disable CS1591
using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Lessons;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Lessons;
using LearningPortal.Shared.Results;
namespace LearningPortal.Application.Lessons.Queries.GetLessonById;
public sealed class GetLessonByIdQueryHandler(ILessonRepository lessons, ICourseRepository courses, ICurrentUserService user,
    IVideoEmbedResolver videos, IMarkdownRenderer markdown)
    : IQueryHandler<GetLessonByIdQuery, Result<LessonResponse>>
{
    public async Task<Result<LessonResponse>> HandleAsync(GetLessonByIdQuery q, CancellationToken ct = default)
    {
        var lesson = await lessons.GetByIdReadOnlyAsync(q.LessonId, ct);
        if (lesson is null) return Result<LessonResponse>.Failure(Errors.LessonManagement.NotFound(q.LessonId));
        var course = await courses.GetByIdReadOnlyAsync(lesson.CourseId, ct);
        var error = course is null ? Errors.LessonManagement.NotFound(q.LessonId) : LessonSupport.Authorize(user, course);
        return error is null ? Result<LessonResponse>.Success(lesson.ToResponse(videos, markdown)) : Result<LessonResponse>.Failure(error);
    }
}
