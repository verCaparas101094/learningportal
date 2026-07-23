using FluentValidation;
using LearningPortal.Domain.Courses;

namespace LearningPortal.Application.Courses.Commands.UpdateCourse;

/// <summary>Validates Draft course updates.</summary>
public sealed class UpdateCourseCommandValidator : AbstractValidator<UpdateCourseCommand>
{
    /// <summary>Initializes update rules.</summary>
    public UpdateCourseCommandValidator()
    {
        RuleFor(command => command.CourseId).NotEmpty();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Slug)
            .NotEmpty()
            .MaximumLength(200)
            .Must(slug => SlugNormalizer.Normalize(slug).Length > 0)
            .WithMessage("Slug must contain at least one letter or number.");
        RuleFor(command => command.Description).NotNull().MaximumLength(5_000);
        RuleFor(command => command.Category).NotEmpty().MaximumLength(100);
        RuleFor(command => command.ThumbnailUrl).MaximumLength(2_048);
        RuleFor(command => command.RowVersion)
            .NotEmpty()
            .Must(IsBase64)
            .WithMessage("RowVersion must be a valid Base64 value.");
    }

    private static bool IsBase64(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        Span<byte> buffer = stackalloc byte[value.Length];
        return Convert.TryFromBase64String(value, buffer, out var bytesWritten)
               && bytesWritten > 0;
    }
}
