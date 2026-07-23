using LearningPortal.Application.Lessons.Commands.CreateLesson;
using Xunit;

namespace LearningPortal.Infrastructure.Tests.Lessons;

/// <summary>Verifies lesson command validation.</summary>
public sealed class LessonValidationTests
{
    /// <summary>Verifies required fields and numeric constraints.</summary>
    [Fact]
    public async Task Create_InvalidValues_FailsValidation()
    {
        var command = new CreateLessonCommand(Guid.Empty, "", new string('x', 2001), "", 0, 0, "Unknown");
        var result = await new CreateLessonCommandValidator().ValidateAsync(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CreateLessonCommand.Title));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CreateLessonCommand.Order));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CreateLessonCommand.LessonType));
    }
}
