using FluentValidation;

namespace LearningPortal.Application.Courses.Commands.ArchiveCourse;

/// <summary>Validates archival requests.</summary>
public sealed class ArchiveCourseCommandValidator : AbstractValidator<ArchiveCourseCommand>
{
    /// <summary>Initializes archival rules.</summary>
    public ArchiveCourseCommandValidator() =>
        RuleFor(command => command.CourseId).NotEmpty();
}
