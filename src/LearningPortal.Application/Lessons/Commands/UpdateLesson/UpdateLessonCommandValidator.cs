#pragma warning disable CS1591
using FluentValidation;
using LearningPortal.Domain.Lessons;
namespace LearningPortal.Application.Lessons.Commands.UpdateLesson;
public sealed class UpdateLessonCommandValidator : AbstractValidator<UpdateLessonCommand>
{
    public UpdateLessonCommandValidator()
    {
        RuleFor(x => x.LessonId).NotEmpty(); RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotNull().MaximumLength(2_000); RuleFor(x => x.MarkdownContent).MaximumLength(100_000);
        RuleFor(x => x.Order).GreaterThanOrEqualTo(1); RuleFor(x => x.EstimatedMinutes).GreaterThan(0);
        RuleFor(x => x.LessonType).Must(x => Enum.TryParse<LessonType>(x, true, out var t) && Enum.IsDefined(t));
        RuleFor(x => x.RowVersion).NotEmpty().Must(x => LessonSupport.TryRowVersion(x, out _));
        RuleFor(x => x).Must(HasMatchingContent).WithMessage("Content must match the selected lesson type.");
    }
    private static bool HasMatchingContent(UpdateLessonCommand x) =>
        Enum.TryParse<LessonType>(x.LessonType, true, out var type) &&
        (type == LessonType.Article
            ? !string.IsNullOrWhiteSpace(x.MarkdownContent) && x.ExternalUrl is null
            : x.MarkdownContent is null && !string.IsNullOrWhiteSpace(x.ExternalUrl));
}
