#pragma warning disable CS1591
using LearningPortal.Infrastructure.Lessons;
using Xunit;

namespace LearningPortal.Infrastructure.Tests.Lessons;

/// <summary>Verifies deterministic video source resolution.</summary>
public sealed class VideoEmbedResolverTests
{
    private readonly VideoEmbedResolver resolver = new();

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=abc12345", "YouTube", "https://www.youtube-nocookie.com/embed/abc12345")]
    [InlineData("https://youtu.be/abc12345", "YouTube", "https://www.youtube-nocookie.com/embed/abc12345")]
    [InlineData("https://www.youtube.com/embed/abc12345", "YouTube", "https://www.youtube-nocookie.com/embed/abc12345")]
    [InlineData("https://vimeo.com/123456", "Vimeo", "https://player.vimeo.com/video/123456")]
    [InlineData("https://player.vimeo.com/video/123456", "Vimeo", "https://player.vimeo.com/video/123456")]
    public void SupportedProvider_Resolves(string url, string provider, string embed)
    {
        var result = resolver.Resolve(url);
        Assert.True(result.IsSuccess);
        Assert.Equal(provider, result.Value.VideoProvider);
        Assert.Equal(embed, result.Value.EmbedUrl);
    }

    [Fact]
    public void MicrosoftSharePoint_Resolves() =>
        Assert.Equal("MicrosoftStream", resolver.Resolve("https://tenant.sharepoint.com/sites/training/video").Value.VideoProvider);

    [Fact]
    public void DirectMp4_UsesVideoElementMetadata()
    {
        var result = resolver.Resolve("https://cdn.example.com/training/video.MP4?token=1");
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsDirectVideo);
    }

    [Theory]
    [InlineData("http://youtube.com/watch?v=abc12345")]
    [InlineData("https://youtube.com.example.com/watch?v=abc12345")]
    [InlineData("https://vimeo.com.attacker.net/123456")]
    [InlineData("javascript:alert(1)")]
    [InlineData("not a url")]
    public void UnsafeOrUnsupportedUrl_IsRejected(string url) => Assert.True(resolver.Resolve(url).IsFailure);
}
