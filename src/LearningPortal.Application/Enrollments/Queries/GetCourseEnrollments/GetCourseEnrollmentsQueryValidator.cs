using FluentValidation;
using LearningPortal.Domain.Enrollments;

namespace LearningPortal.Application.Enrollments.Queries.GetCourseEnrollments;

/// <summary>Validates course enrollment filters.</summary>
public sealed class GetCourseEnrollmentsQueryValidator : AbstractValidator<GetCourseEnrollmentsQuery>
{
    /// <summary>Creates validation rules.</summary>
    public GetCourseEnrollmentsQueryValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(200);
        RuleFor(x => x.Status).Must(value => string.IsNullOrWhiteSpace(value)
            || Enum.TryParse<EnrollmentStatus>(value, true, out _)).WithMessage("Status is invalid.");
    }
}
