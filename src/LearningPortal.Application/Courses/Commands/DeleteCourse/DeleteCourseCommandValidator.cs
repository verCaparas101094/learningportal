using FluentValidation;

namespace LearningPortal.Application.Courses.Commands.DeleteCourse;

/// <summary>Validates course deletion requests.</summary>
public sealed class DeleteCourseCommandValidator : AbstractValidator<DeleteCourseCommand>
{
    /// <summary>Initializes deletion rules.</summary>
    public DeleteCourseCommandValidator() =>
        RuleFor(command => command.CourseId).NotEmpty();
}
