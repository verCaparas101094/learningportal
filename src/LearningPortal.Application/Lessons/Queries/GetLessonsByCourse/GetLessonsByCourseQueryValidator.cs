#pragma warning disable CS1591
using FluentValidation;
namespace LearningPortal.Application.Lessons.Queries.GetLessonsByCourse;
public sealed class GetLessonsByCourseQueryValidator : AbstractValidator<GetLessonsByCourseQuery>
{
    public GetLessonsByCourseQueryValidator()
    { RuleFor(x => x.CourseId).NotEmpty(); RuleFor(x => x.Search).MaximumLength(200);
      RuleFor(x => x.PageNumber).GreaterThan(0); RuleFor(x => x.PageSize).InclusiveBetween(1, 100); }
}
