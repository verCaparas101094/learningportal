#pragma warning disable CS1591
using FluentValidation;
namespace LearningPortal.Application.Lessons.Commands.DeleteLesson;
public sealed class DeleteLessonCommandValidator : AbstractValidator<DeleteLessonCommand>
{
    public DeleteLessonCommandValidator() => RuleFor(x => x.LessonId).NotEmpty();
}
