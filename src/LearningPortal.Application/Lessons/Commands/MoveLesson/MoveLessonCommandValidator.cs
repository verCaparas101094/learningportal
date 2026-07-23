#pragma warning disable CS1591
using FluentValidation;
namespace LearningPortal.Application.Lessons.Commands.MoveLesson;
public sealed class MoveLessonCommandValidator : AbstractValidator<MoveLessonCommand>
{
    public MoveLessonCommandValidator()
    { RuleFor(x => x.LessonId).NotEmpty(); RuleFor(x => x.NewOrder).GreaterThanOrEqualTo(1);
      RuleFor(x => x.RowVersion).NotEmpty().Must(x => LessonSupport.TryRowVersion(x, out _)); }
}
