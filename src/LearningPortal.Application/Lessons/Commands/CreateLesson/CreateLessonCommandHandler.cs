#pragma warning disable CS1591
using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Domain.Lessons;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Lessons;
using LearningPortal.Shared.Results;
namespace LearningPortal.Application.Lessons.Commands.CreateLesson;
public sealed class CreateLessonCommandHandler(ILessonRepository lessons, ICourseRepository courses,
    IUnitOfWork unit, ICurrentUserService user) : ICommandHandler<CreateLessonCommand, Result<LessonResponse>>
{
    public async Task<Result<LessonResponse>> HandleAsync(CreateLessonCommand c, CancellationToken ct = default)
    {
        var course = await courses.GetByIdReadOnlyAsync(c.CourseId, ct);
        if (course is null) return Result<LessonResponse>.Failure(Errors.CourseManagement.NotFound(c.CourseId));
        var error = LessonSupport.Authorize(user, course);
        if (error is not null) return Result<LessonResponse>.Failure(error);
        if (await lessons.OrderExistsAsync(c.CourseId, c.Order, cancellationToken: ct))
            return Result<LessonResponse>.Failure(Errors.LessonManagement.DuplicateOrder());
        var lesson = Lesson.Create(c.CourseId, c.Title, c.Description, c.Content, c.Order, c.EstimatedMinutes,
            Enum.Parse<LessonType>(c.LessonType, true));
        await lessons.AddAsync(lesson, ct);
        error = await LessonSupport.SaveAsync(unit, ct);
        return error is null ? Result<LessonResponse>.Success(lesson.ToResponse()) : Result<LessonResponse>.Failure(error);
    }
}
