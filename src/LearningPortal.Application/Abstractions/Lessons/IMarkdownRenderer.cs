namespace LearningPortal.Application.Abstractions.Lessons;

/// <summary>Renders Markdown source into sanitized HTML.</summary>
public interface IMarkdownRenderer
{
    /// <summary>Renders safe HTML from Markdown source.</summary>
    string Render(string markdown);
}
