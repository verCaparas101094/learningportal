#pragma warning disable CS1591
using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Domain.Lessons;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Lessons;
using LearningPortal.Shared.Results;
namespace LearningPortal.Application.Lessons.Commands.UpdateLesson;
public sealed class UpdateLessonCommandHandler(ILessonRepository lessons, ICourseRepository courses, IUnitOfWork unit,
    ICurrentUserService user) : ICommandHandler<UpdateLessonCommand, Result<LessonResponse>>
{
    public async Task<Result<LessonResponse>> HandleAsync(UpdateLessonCommand c, CancellationToken ct = default)
    {
        var lesson = await lessons.GetByIdAsync(c.LessonId, ct);
        if (lesson is null) return Result<LessonResponse>.Failure(Errors.LessonManagement.NotFound(c.LessonId));
        var course = await courses.GetByIdReadOnlyAsync(lesson.CourseId, ct);
        if (course is null) return Result<LessonResponse>.Failure(Errors.LessonManagement.NotFound(c.LessonId));
        var error = LessonSupport.Authorize(user, course);
        if (error is not null) return Result<LessonResponse>.Failure(error);
        if (await lessons.OrderExistsAsync(lesson.CourseId, c.Order, lesson.Id, ct))
            return Result<LessonResponse>.Failure(Errors.LessonManagement.DuplicateOrder());
        if (!lesson.TryUpdate(c.Title, c.Description, c.Content, c.Order, c.EstimatedMinutes,
                Enum.Parse<LessonType>(c.LessonType, true)))
            return Result<LessonResponse>.Failure(Errors.LessonManagement.InvalidState("updated"));
        LessonSupport.TryRowVersion(c.RowVersion, out var version); lessons.SetOriginalRowVersion(lesson, version);
        error = await LessonSupport.SaveAsync(unit, ct);
        return error is null ? Result<LessonResponse>.Success(lesson.ToResponse()) : Result<LessonResponse>.Failure(error);
    }
}
