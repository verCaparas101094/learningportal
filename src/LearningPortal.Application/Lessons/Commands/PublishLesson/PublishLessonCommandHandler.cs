#pragma warning disable CS1591
using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Lessons;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Lessons;
using LearningPortal.Shared.Results;
namespace LearningPortal.Application.Lessons.Commands.PublishLesson;
public sealed class PublishLessonCommandHandler(ILessonRepository lessons, ICourseRepository courses, IUnitOfWork unit,
    ICurrentUserService user, IVideoEmbedResolver videos, IMarkdownRenderer markdown)
    : ICommandHandler<PublishLessonCommand, Result<LessonResponse>>
{
    public async Task<Result<LessonResponse>> HandleAsync(PublishLessonCommand c, CancellationToken ct = default)
    {
        var lesson = await lessons.GetByIdAsync(c.LessonId, ct);
        if (lesson is null) return Result<LessonResponse>.Failure(Errors.LessonManagement.NotFound(c.LessonId));
        var course = await courses.GetByIdReadOnlyAsync(lesson.CourseId, ct);
        var error = course is null ? Errors.LessonManagement.NotFound(c.LessonId) : LessonSupport.Authorize(user, course);
        if (error is not null) return Result<LessonResponse>.Failure(error);
        if (!lesson.TryPublish()) return Result<LessonResponse>.Failure(Errors.LessonManagement.InvalidState("published"));
        error = await LessonSupport.SaveAsync(unit, ct);
        return error is null ? Result<LessonResponse>.Success(lesson.ToResponse(videos, markdown)) : Result<LessonResponse>.Failure(error);
    }
}
