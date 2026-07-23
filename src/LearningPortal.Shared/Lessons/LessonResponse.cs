namespace LearningPortal.Shared.Lessons;

/// <summary>Represents complete lesson management details and safe preview data.</summary>
public sealed record LessonResponse(Guid Id, Guid CourseId, string Title, string Description, int Order,
    int EstimatedMinutes, string LessonType, string? MarkdownContent, string? ExternalUrl, string VideoProvider,
    string? EmbedUrl, bool IsDirectVideo, string? ContentPreview, string Status, DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc, string RowVersion);
