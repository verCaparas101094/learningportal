#pragma warning disable CS1591
using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Results;
namespace LearningPortal.Application.Lessons.Commands.DeleteLesson;
public sealed class DeleteLessonCommandHandler(ILessonRepository lessons, ICourseRepository courses, IUnitOfWork unit,
    ICurrentUserService user) : ICommandHandler<DeleteLessonCommand, Result<bool>>
{
    public async Task<Result<bool>> HandleAsync(DeleteLessonCommand c, CancellationToken ct = default)
    {
        var lesson = await lessons.GetByIdAsync(c.LessonId, ct);
        if (lesson is null) return Result<bool>.Failure(Errors.LessonManagement.NotFound(c.LessonId));
        var course = await courses.GetByIdReadOnlyAsync(lesson.CourseId, ct);
        var error = course is null ? Errors.LessonManagement.NotFound(c.LessonId) : LessonSupport.Authorize(user, course);
        if (error is not null) return Result<bool>.Failure(error);
        if (!lesson.TryDelete()) return Result<bool>.Failure(Errors.LessonManagement.InvalidState("deleted"));
        lessons.Remove(lesson); error = await LessonSupport.SaveAsync(unit, ct);
        return error is null ? Result<bool>.Success(true) : Result<bool>.Failure(error);
    }
}
