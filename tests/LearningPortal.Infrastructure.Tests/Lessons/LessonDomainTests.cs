#pragma warning disable CS1591
using LearningPortal.Domain.Lessons;
using Xunit;

namespace LearningPortal.Infrastructure.Tests.Lessons;

/// <summary>Verifies lesson content and lifecycle rules.</summary>
public sealed class LessonDomainTests
{
    [Fact]
    public void Article_RequiresMarkdown() =>
        Assert.Throws<ArgumentException>(() => Create(LessonType.Article, null, null, VideoProvider.None));

    [Fact]
    public void Video_RequiresSupportedDerivedProvider() =>
        Assert.Throws<ArgumentException>(() => Create(LessonType.Video, null, "https://example.com/video", VideoProvider.None));

    [Theory]
    [InlineData(LessonType.Pdf)]
    [InlineData(LessonType.ExternalLink)]
    public void ExternalContent_RequiresHttps(LessonType type) =>
        Assert.Throws<ArgumentException>(() => Create(type, null, "http://example.com/file", VideoProvider.None));

    [Fact]
    public void TypeTransition_ClearsIrrelevantFields()
    {
        var lesson = Create(LessonType.Article, "# Article", null, VideoProvider.None);
        Assert.True(lesson.TryUpdate("Title", "", 1, 10, LessonType.Video, null,
            "https://www.youtube.com/watch?v=abc12345", VideoProvider.YouTube));
        Assert.Null(lesson.MarkdownContent);
        Assert.Equal(VideoProvider.YouTube, lesson.VideoProvider);
    }

    [Fact]
    public void RawEmbedMarkup_IsRejected() =>
        Assert.Throws<ArgumentException>(() => Create(LessonType.Article, "<iframe src='x'></iframe>", null, VideoProvider.None));

    [Fact]
    public void ValidDraft_PublishesAndBecomesReadOnly()
    {
        var lesson = Create(LessonType.Article, "# Article", null, VideoProvider.None);
        Assert.True(lesson.TryPublish());
        Assert.False(lesson.TryUpdate("Changed", "", 1, 10, LessonType.Article, "# Changed", null, VideoProvider.None));
        Assert.False(lesson.TryDelete());
    }

    private static Lesson Create(LessonType type, string? markdown, string? url, VideoProvider provider) =>
        Lesson.Create(Guid.NewGuid(), "Title", "", 1, 10, type, markdown, url, provider);
}
