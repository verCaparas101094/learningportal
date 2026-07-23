namespace LearningPortal.Shared.Lessons;

/// <summary>Contains transient lesson content to validate and preview.</summary>
public sealed record LessonContentPreviewRequest(string LessonType, string? MarkdownContent, string? ExternalUrl);
