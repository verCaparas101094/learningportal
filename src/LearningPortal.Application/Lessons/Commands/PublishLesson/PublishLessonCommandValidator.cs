#pragma warning disable CS1591
using FluentValidation;
namespace LearningPortal.Application.Lessons.Commands.PublishLesson;
public sealed class PublishLessonCommandValidator : AbstractValidator<PublishLessonCommand>
{
    public PublishLessonCommandValidator() => RuleFor(x => x.LessonId).NotEmpty();
}
