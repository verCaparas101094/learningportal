using LearningPortal.Domain.Common;

namespace LearningPortal.Domain.Lessons;

/// <summary>Represents one ordered course lesson.</summary>
public sealed class Lesson : AuditableEntity, ISoftDelete
{
    private Lesson() { }

    private Lesson(Guid courseId, string title, string description, int order, int estimatedMinutes,
        LessonType lessonType, string? markdownContent, string? externalUrl, VideoProvider videoProvider)
    {
        CourseId = courseId;
        SetContent(title, description, order, estimatedMinutes, lessonType, markdownContent, externalUrl, videoProvider);
        Status = LessonStatus.Draft;
    }

    /// <summary>Gets the owning course identifier.</summary>
    public Guid CourseId { get; private set; }
    /// <summary>Gets the title.</summary>
    public string Title { get; private set; } = string.Empty;
    /// <summary>Gets the description.</summary>
    public string Description { get; private set; } = string.Empty;
    /// <summary>Gets the course-relative order.</summary>
    public int Order { get; private set; }
    /// <summary>Gets the estimated duration.</summary>
    public int EstimatedMinutes { get; private set; }
    /// <summary>Gets the content type.</summary>
    public LessonType LessonType { get; private set; }
    /// <summary>Gets Markdown source for an article.</summary>
    public string? MarkdownContent { get; private set; }
    /// <summary>Gets the normalized external source URL.</summary>
    public string? ExternalUrl { get; private set; }
    /// <summary>Gets the server-derived video provider.</summary>
    public VideoProvider VideoProvider { get; private set; }
    /// <summary>Gets the lifecycle status.</summary>
    public LessonStatus Status { get; private set; }
    /// <inheritdoc />
    public bool IsDeleted { get; private set; }
    /// <inheritdoc />
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    /// <inheritdoc />
    public Guid? DeletedBy { get; private set; }

    /// <summary>Creates a valid Draft lesson.</summary>
    public static Lesson Create(Guid courseId, string title, string description, int order, int estimatedMinutes,
        LessonType lessonType, string? markdownContent, string? externalUrl, VideoProvider videoProvider)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(courseId, Guid.Empty);
        Validate(title, description, order, estimatedMinutes, lessonType, markdownContent, externalUrl, videoProvider);
        return new(courseId, title, description, order, estimatedMinutes, lessonType, markdownContent, externalUrl, videoProvider);
    }

    /// <summary>Updates editable Draft lesson details.</summary>
    public bool TryUpdate(string title, string description, int order, int estimatedMinutes, LessonType lessonType,
        string? markdownContent, string? externalUrl, VideoProvider videoProvider)
    {
        if (Status != LessonStatus.Draft ||
            !IsValid(title, description, order, estimatedMinutes, lessonType, markdownContent, externalUrl, videoProvider))
            return false;
        SetContent(title, description, order, estimatedMinutes, lessonType, markdownContent, externalUrl, videoProvider);
        return true;
    }

    /// <summary>Publishes a complete Draft lesson.</summary>
    public bool TryPublish()
    {
        if (Status != LessonStatus.Draft ||
            !IsValid(Title, Description, Order, EstimatedMinutes, LessonType, MarkdownContent, ExternalUrl, VideoProvider))
            return false;
        Status = LessonStatus.Published;
        return true;
    }

    /// <summary>Prepares a Draft lesson for soft deletion.</summary>
    public bool TryDelete() => Status == LessonStatus.Draft;

    private void SetContent(string title, string description, int order, int minutes, LessonType type,
        string? markdown, string? url, VideoProvider provider)
    {
        Title = title.Trim(); Description = description.Trim(); Order = order; EstimatedMinutes = minutes; LessonType = type;
        MarkdownContent = type == LessonType.Article ? markdown?.Trim() : null;
        ExternalUrl = type == LessonType.Article ? null : url?.Trim();
        VideoProvider = type == LessonType.Video ? provider : VideoProvider.None;
    }

    private static void Validate(string title, string description, int order, int minutes, LessonType type,
        string? markdown, string? url, VideoProvider provider)
    {
        if (!IsValid(title, description, order, minutes, type, markdown, url, provider))
            throw new ArgumentException("Lesson content is invalid for the selected lesson type.");
    }

    private static bool IsValid(string title, string description, int order, int minutes, LessonType type,
        string? markdown, string? url, VideoProvider provider)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 200 || description is null ||
            description.Length > 2_000 || order < 1 || minutes <= 0 || !Enum.IsDefined(type)) return false;
        if (type == LessonType.Article)
            return !string.IsNullOrWhiteSpace(markdown) && markdown.Length <= 100_000 &&
                   url is null && provider == VideoProvider.None && !ContainsEmbedMarkup(markdown);
        if (!IsAbsoluteHttps(url) || markdown is not null || ContainsEmbedMarkup(url!)) return false;
        return type == LessonType.Video
            ? IsValidVideoProvider(url!, provider)
            : provider == VideoProvider.None;
    }

    private static bool IsAbsoluteHttps(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps &&
        string.IsNullOrEmpty(uri.UserInfo);

    private static bool ContainsEmbedMarkup(string value) =>
        value.Contains("<iframe", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("<embed", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("<object", StringComparison.OrdinalIgnoreCase);

    private static bool IsValidVideoProvider(string value, VideoProvider provider)
    {
        var uri = new Uri(value);
        var host = uri.IdnHost;
        return provider switch
        {
            VideoProvider.YouTube => host.Equals("youtube.com", StringComparison.OrdinalIgnoreCase) ||
                                     host.Equals("www.youtube.com", StringComparison.OrdinalIgnoreCase) ||
                                     host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase),
            VideoProvider.Vimeo => host.Equals("vimeo.com", StringComparison.OrdinalIgnoreCase) ||
                                   host.Equals("www.vimeo.com", StringComparison.OrdinalIgnoreCase) ||
                                   host.Equals("player.vimeo.com", StringComparison.OrdinalIgnoreCase),
            VideoProvider.MicrosoftStream => host.Equals("stream.microsoft.com", StringComparison.OrdinalIgnoreCase) ||
                                             host.Equals("web.microsoftstream.com", StringComparison.OrdinalIgnoreCase) ||
                                             host.EndsWith(".microsoftstream.com", StringComparison.OrdinalIgnoreCase) ||
                                             host.EndsWith(".sharepoint.com", StringComparison.OrdinalIgnoreCase),
            VideoProvider.DirectMp4 => uri.AbsolutePath.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }
}
