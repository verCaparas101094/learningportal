#pragma warning disable CS1591
using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Lessons;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Lessons;
using LearningPortal.Shared.Results;
namespace LearningPortal.Application.Lessons.Commands.MoveLesson;
public sealed class MoveLessonCommandHandler(ILessonRepository lessons, ICourseRepository courses, ICurrentUserService user,
    IVideoEmbedResolver videos, IMarkdownRenderer markdown)
    : ICommandHandler<MoveLessonCommand, Result<LessonResponse>>
{
    public async Task<Result<LessonResponse>> HandleAsync(MoveLessonCommand c, CancellationToken ct = default)
    {
        var lesson = await lessons.GetByIdReadOnlyAsync(c.LessonId, ct);
        if (lesson is null) return Result<LessonResponse>.Failure(Errors.LessonManagement.NotFound(c.LessonId));
        var course = await courses.GetByIdReadOnlyAsync(lesson.CourseId, ct);
        var error = course is null ? Errors.LessonManagement.NotFound(c.LessonId) : LessonSupport.Authorize(user, course);
        if (error is not null) return Result<LessonResponse>.Failure(error);
        LessonSupport.TryRowVersion(c.RowVersion, out var version);
        var result = await lessons.MoveAsync(c.LessonId, c.NewOrder, version, ct);
        if (result != LessonMoveResult.Moved)
            return Result<LessonResponse>.Failure(result == LessonMoveResult.ConcurrencyConflict
                ? Errors.LessonManagement.ConcurrencyConflict() : Errors.LessonManagement.InvalidOrder());
        var updated = await lessons.GetByIdReadOnlyAsync(c.LessonId, ct);
        return updated is null ? Result<LessonResponse>.Failure(Errors.LessonManagement.NotFound(c.LessonId))
            : Result<LessonResponse>.Success(updated.ToResponse(videos, markdown));
    }
}
