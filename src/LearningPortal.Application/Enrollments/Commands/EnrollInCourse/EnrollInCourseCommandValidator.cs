using FluentValidation;

namespace LearningPortal.Application.Enrollments.Commands.EnrollInCourse;

/// <summary>Validates enrollment input.</summary>
public sealed class EnrollInCourseCommandValidator : AbstractValidator<EnrollInCourseCommand>
{
    /// <summary>Creates validation rules.</summary>
    public EnrollInCourseCommandValidator() => RuleFor(x => x.CourseId).NotEmpty();
}
