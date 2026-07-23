using databinding.WebSecurity.HtmlSanitizer;
using databinding.WebSecurity.HtmlSanitizer.Settings;
using LearningPortal.Application.Abstractions.Lessons;
using Markdig;

namespace LearningPortal.Infrastructure.Lessons;

/// <summary>Renders advanced Markdown through an explicit HTML allowlist.</summary>
public sealed class MarkdownRenderer : IMarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();
    private readonly HtmlSanitizer sanitizer = CreateSanitizer();

    /// <inheritdoc />
    public string Render(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        var cleanedSource = sanitizer.Sanitize(markdown);
        var sanitizedHtml = sanitizer.Sanitize(Markdown.ToHtml(cleanedSource, Pipeline));
        return sanitizedHtml.Replace(
            "<a href=",
            "<a target=\"_blank\" rel=\"noopener noreferrer\" href=",
            StringComparison.Ordinal);
    }

    private static HtmlSanitizer CreateSanitizer()
    {
        var options = new HtmlSanitizerOptions
        {
            AllowedTags = new HashSet<string>(["h1", "h2", "h3", "h4", "h5", "h6", "p", "strong", "em", "ul", "ol",
                "li", "a", "blockquote", "code", "pre", "table", "thead", "tbody", "tr", "th", "td", "hr", "br"]),
            AllowedAttributes = new HashSet<string>(["href", "title"]),
            UriAttributes = new HashSet<string>(["href"])
        };
        return new HtmlSanitizer(options);
    }
}
