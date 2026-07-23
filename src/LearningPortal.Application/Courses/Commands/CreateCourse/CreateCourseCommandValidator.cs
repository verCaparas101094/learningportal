using FluentValidation;
using LearningPortal.Domain.Courses;

namespace LearningPortal.Application.Courses.Commands.CreateCourse;

/// <summary>Validates course creation input.</summary>
public sealed class CreateCourseCommandValidator : AbstractValidator<CreateCourseCommand>
{
    /// <summary>Initializes course creation rules.</summary>
    public CreateCourseCommandValidator()
    {
        RuleFor(command => command.Title).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Slug)
            .NotEmpty()
            .MaximumLength(200)
            .Must(slug => SlugNormalizer.Normalize(slug).Length > 0)
            .WithMessage("Slug must contain at least one letter or number.");
        RuleFor(command => command.Description).NotNull().MaximumLength(5_000);
        RuleFor(command => command.Category).NotEmpty().MaximumLength(100);
        RuleFor(command => command.ThumbnailUrl).MaximumLength(2_048);
        RuleFor(command => command.InstructorId)
            .Must(instructorId => instructorId is null || instructorId != Guid.Empty);
    }
}
