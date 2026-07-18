using FluentValidation;

namespace LearningPortal.Application.Courses.Commands.CreateCourse;

/// <summary>Validates course creation input before domain execution.</summary>
public sealed class CreateCourseCommandValidator : AbstractValidator<CreateCourseCommand>
{
    /// <summary>Initializes course creation rules.</summary>
    public CreateCourseCommandValidator()
    {
        RuleFor(command => command.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.Description)
            .NotNull()
            .MaximumLength(2_000);
    }
}
