#pragma warning disable CS1591
using LearningPortal.Application.Lessons.Commands.CreateLesson;
using Xunit;

namespace LearningPortal.Infrastructure.Tests.Lessons;

/// <summary>Verifies lesson command validation.</summary>
public sealed class LessonValidationTests
{
    [Fact]
    public async Task ArticleWithoutMarkdown_FailsValidation()
    {
        var command = new CreateLessonCommand(Guid.NewGuid(), "Title", "", 1, 10, "Article", null, null);
        var result = await new CreateLessonCommandValidator().ValidateAsync(command);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task VideoWithMarkdown_FailsValidation()
    {
        var command = new CreateLessonCommand(Guid.NewGuid(), "Title", "", 1, 10, "Video", "text", "https://youtu.be/abc12345");
        var result = await new CreateLessonCommandValidator().ValidateAsync(command);
        Assert.False(result.IsValid);
    }
}
