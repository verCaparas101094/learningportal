#pragma warning disable CS1591
using LearningPortal.Infrastructure.Lessons;
using Xunit;

namespace LearningPortal.Infrastructure.Tests.Lessons;

/// <summary>Verifies Markdown rendering and sanitization.</summary>
public sealed class MarkdownRendererTests
{
    private readonly MarkdownRenderer renderer = new();

    [Fact]
    public void SafeFormatting_IsRetained()
    {
        var html = renderer.Render("# Heading\n\n**bold**\n\n```csharp\nvar x = 1;\n```");
        Assert.Contains("<h1", html); Assert.Contains("<strong>bold</strong>", html); Assert.Contains("<code", html);
    }

    [Theory]
    [InlineData("<script>alert(1)</script>", "script")]
    [InlineData("<iframe src=\"https://example.com\"></iframe>", "iframe")]
    [InlineData("<p onclick=\"alert(1)\">text</p>", "onclick")]
    [InlineData("[click](javascript:alert(1))", "javascript:")]
    public void UnsafeHtml_IsRemoved(string markdown, string forbidden) =>
        Assert.DoesNotContain(forbidden, renderer.Render(markdown), StringComparison.OrdinalIgnoreCase);
}
