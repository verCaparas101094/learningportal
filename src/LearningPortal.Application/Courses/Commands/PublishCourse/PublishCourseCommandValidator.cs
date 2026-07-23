using FluentValidation;

namespace LearningPortal.Application.Courses.Commands.PublishCourse;

/// <summary>Validates publication requests.</summary>
public sealed class PublishCourseCommandValidator : AbstractValidator<PublishCourseCommand>
{
    /// <summary>Initializes publication rules.</summary>
    public PublishCourseCommandValidator() =>
        RuleFor(command => command.CourseId).NotEmpty();
}
