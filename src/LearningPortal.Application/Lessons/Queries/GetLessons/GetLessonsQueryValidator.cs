#pragma warning disable CS1591
using FluentValidation;
namespace LearningPortal.Application.Lessons.Queries.GetLessons;
public sealed class GetLessonsQueryValidator : AbstractValidator<GetLessonsQuery>
{
    public GetLessonsQueryValidator()
    { RuleFor(x => x.Search).MaximumLength(200); RuleFor(x => x.PageNumber).GreaterThan(0); RuleFor(x => x.PageSize).InclusiveBetween(1, 100); }
}
