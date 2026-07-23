#pragma warning disable CS1591
using FluentValidation;
using LearningPortal.Domain.Lessons;
namespace LearningPortal.Application.Lessons.Commands.UpdateLesson;
public sealed class UpdateLessonCommandValidator : AbstractValidator<UpdateLessonCommand>
{
    public UpdateLessonCommandValidator()
    {
        RuleFor(x => x.LessonId).NotEmpty(); RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotNull().MaximumLength(2_000); RuleFor(x => x.Content).NotEmpty().MaximumLength(100_000);
        RuleFor(x => x.Order).GreaterThanOrEqualTo(1); RuleFor(x => x.EstimatedMinutes).GreaterThan(0);
        RuleFor(x => x.LessonType).Must(x => Enum.TryParse<LessonType>(x, true, out _));
        RuleFor(x => x.RowVersion).NotEmpty().Must(x => LessonSupport.TryRowVersion(x, out _));
    }
}
