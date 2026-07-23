using FluentValidation;
using LearningPortal.Domain.Courses;

namespace LearningPortal.Application.Courses.Queries.GetCourses;

/// <summary>Validates course-list filters and pagination.</summary>
public sealed class GetCoursesQueryValidator : AbstractValidator<GetCoursesQuery>
{
    /// <summary>Initializes list-query rules.</summary>
    public GetCoursesQueryValidator()
    {
        RuleFor(query => query.Search).MaximumLength(200);
        RuleFor(query => query.Status)
            .Must(status => string.IsNullOrWhiteSpace(status)
                            || Enum.TryParse<CourseStatus>(status, true, out _))
            .WithMessage("Status must be Draft, Published, or Archived.");
        RuleFor(query => query.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
