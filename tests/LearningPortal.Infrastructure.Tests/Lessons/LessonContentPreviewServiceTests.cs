#pragma warning disable CS1591
using LearningPortal.Application.Lessons;
using LearningPortal.Infrastructure.Lessons;
using LearningPortal.Shared.Lessons;
using Xunit;

namespace LearningPortal.Infrastructure.Tests.Lessons;

/// <summary>Verifies application lesson preview behavior.</summary>
public sealed class LessonContentPreviewServiceTests
{
    private readonly LessonContentPreviewService service = new(new VideoEmbedResolver(), new MarkdownRenderer());

    [Theory]
    [InlineData("Article", "# Article", null)]
    [InlineData("Video", null, "https://youtu.be/abc12345")]
    [InlineData("Pdf", null, "https://example.com/file.pdf")]
    [InlineData("ExternalLink", null, "https://example.com/resource")]
    public void SupportedType_ReturnsSafePreview(string type, string? markdown, string? url)
    {
        var result = service.Preview(new LessonContentPreviewRequest(type, markdown, url));
        Assert.True(result.IsSuccess);
        Assert.Equal(type, result.Value.LessonType);
    }

    [Fact]
    public void UnsupportedVideo_ReturnsValidationFailure()
    {
        var result = service.Preview(new("Video", null, "https://youtube.com.example.com/watch?v=abc12345"));
        Assert.True(result.IsFailure);
        Assert.Equal("Lesson.InvalidContent", result.Error!.Code);
    }
}
