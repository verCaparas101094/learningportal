using LearningPortal.Domain.Common;

namespace LearningPortal.Domain.Lessons;

/// <summary>Represents one ordered course lesson.</summary>
public sealed class Lesson : AuditableEntity, ISoftDelete
{
    private Lesson()
    {
    }

    private Lesson(
        Guid courseId,
        string title,
        string description,
        string content,
        int order,
        int estimatedMinutes,
        LessonType lessonType)
    {
        CourseId = courseId;
        Title = title;
        Description = description;
        Content = content;
        Order = order;
        EstimatedMinutes = estimatedMinutes;
        LessonType = lessonType;
        Status = LessonStatus.Draft;
    }

    /// <summary>Gets the owning course identifier.</summary>
    public Guid CourseId { get; private set; }
    /// <summary>Gets the title.</summary>
    public string Title { get; private set; } = string.Empty;
    /// <summary>Gets the description.</summary>
    public string Description { get; private set; } = string.Empty;
    /// <summary>Gets the content.</summary>
    public string Content { get; private set; } = string.Empty;
    /// <summary>Gets the course-relative order.</summary>
    public int Order { get; private set; }
    /// <summary>Gets the estimated duration.</summary>
    public int EstimatedMinutes { get; private set; }
    /// <summary>Gets the content type.</summary>
    public LessonType LessonType { get; private set; }
    /// <summary>Gets the lifecycle status.</summary>
    public LessonStatus Status { get; private set; }
    /// <inheritdoc />
    public bool IsDeleted { get; private set; }
    /// <inheritdoc />
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    /// <inheritdoc />
    public Guid? DeletedBy { get; private set; }

    /// <summary>Creates a Draft lesson.</summary>
    public static Lesson Create(
        Guid courseId,
        string title,
        string description,
        string content,
        int order,
        int estimatedMinutes,
        LessonType lessonType)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(courseId, Guid.Empty);
        Validate(title, description, content, order, estimatedMinutes);
        return new Lesson(
            courseId,
            title.Trim(),
            description.Trim(),
            content.Trim(),
            order,
            estimatedMinutes,
            lessonType);
    }

    /// <summary>Updates editable Draft lesson details.</summary>
    public bool TryUpdate(
        string title,
        string description,
        string content,
        int order,
        int estimatedMinutes,
        LessonType lessonType)
    {
        if (Status != LessonStatus.Draft || !IsValid(title, description, content, order, estimatedMinutes))
        {
            return false;
        }

        Title = title.Trim();
        Description = description.Trim();
        Content = content.Trim();
        Order = order;
        EstimatedMinutes = estimatedMinutes;
        LessonType = lessonType;
        return true;
    }

    /// <summary>Publishes a Draft lesson idempotently.</summary>
    public bool TryPublish()
    {
        if (Status == LessonStatus.Published)
        {
            return true;
        }

        if (Status != LessonStatus.Draft)
        {
            return false;
        }

        Status = LessonStatus.Published;
        return true;
    }

    /// <summary>Prepares a Draft lesson for soft deletion.</summary>
    public bool TryDelete() => Status == LessonStatus.Draft;

    private static void Validate(
        string title,
        string description,
        string content,
        int order,
        int estimatedMinutes)
    {
        if (!IsValid(title, description, content, order, estimatedMinutes))
        {
            throw new ArgumentException("Lesson values are invalid.");
        }
    }

    private static bool IsValid(
        string title,
        string description,
        string content,
        int order,
        int estimatedMinutes) =>
        !string.IsNullOrWhiteSpace(title)
        && title.Trim().Length <= 200
        && description is not null
        && description.Length <= 2_000
        && !string.IsNullOrWhiteSpace(content)
        && content.Length <= 100_000
        && order >= 1
        && estimatedMinutes > 0;
}
