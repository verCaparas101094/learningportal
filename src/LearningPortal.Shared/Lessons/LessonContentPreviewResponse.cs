namespace LearningPortal.Shared.Lessons;

/// <summary>Contains server-validated safe lesson preview data.</summary>
public sealed record LessonContentPreviewResponse(string LessonType, string? SanitizedHtml, string? ExternalUrl,
    string VideoProvider, string? EmbedUrl, bool IsDirectVideo);
